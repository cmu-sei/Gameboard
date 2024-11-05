// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Services;

public class UserService
(
    INowService now,
    SponsorService sponsorService,
    IStore store,
    IStore<Data.User> userStore,
    IMapper mapper,
    IMemoryCache cache,
    INameService namesvc,
    IUserRolePermissionsService permissionsService
)
{
    private readonly IMapper _mapper = mapper;
    private readonly INowService _now = now;
    private readonly SponsorService _sponsorService = sponsorService;
    private readonly IStore _store = store;
    private readonly IStore<Data.User> _userStore = userStore;
    private readonly IMemoryCache _localcache = cache;
    private readonly INameService _namesvc = namesvc;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;

    /// <summary>
    /// If user exists update fields
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<TryCreateUserResult> TryCreate(NewUser model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model.Id);

        var entity = await _store
            .WithNoTracking<Data.User>()
                .Include(u => u.Sponsor)
            .SingleOrDefaultAsync(u => u.Id == model.Id);

        if (entity is not null)
        {
            return new TryCreateUserResult
            {
                IsNewUser = false,
                User = await BuildUserDto(entity)
            };
        }

        entity = _mapper.Map<Data.User>(model);

        // first user gets admin
        if (!await _store.WithNoTracking<Data.User>().AnyAsync(u => u.Id != model.Id))
            entity.Role = UserRoleKey.Admin;

        // record creation date and first login
        if (entity.CreatedOn.IsEmpty())
        {
            entity.CreatedOn = _now.Get();
            entity.LastLoginDate = entity.CreatedOn;
        }

        // if a specific sponsor is requested, try to set it
        if (model.SponsorId.IsNotEmpty())
            entity.SponsorId = await _store
                .WithNoTracking<Data.Sponsor>()
                .Select(s => s.Id)
                .SingleOrDefaultAsync(sId => sId == model.SponsorId);

        // if no sponsor was specified or if the specified one doesn't exist, use the default
        entity.SponsorId ??= (await _sponsorService.GetDefaultSponsor()).Id;

        // unless specifically told otherwise, we flag this user as needing to confirm their sponsor
        entity.HasDefaultSponsor = !model.UnsetDefaultSponsorFlag;

        bool found = false;
        var i = 0;
        do
        {
            entity.ApprovedName = model.DefaultName.IsEmpty() ? _namesvc.GetRandomName() : model.DefaultName.Trim();
            entity.Name = entity.ApprovedName;

            // check uniqueness
            found = await _userStore.AnyAsync(p => p.Id != entity.Id && p.ApprovedName == entity.Name);
        } while (found && i++ < 20);

        await _userStore.Create(entity);
        _localcache.Remove(entity.Id);

        return new TryCreateUserResult
        {
            IsNewUser = true,
            User = await BuildUserDto(entity)
        };
    }

    public async Task<User> Retrieve(string id)
    {
        return await BuildUserDto(await _userStore.Retrieve(id));
    }

    public async Task<User> Update(UpdateUser model, bool canAdminUsers)
    {
        var entity = await _userStore.Retrieve(model.Id, q => q.Include(u => u.Sponsor));
        var sponsorUpdated = false;

        // with user stuff, there are super-users (sudoers) and admins
        // only admins can alter the roles of users
        if (model.Role.HasValue && model.Role != entity.Role)
        {
            if (!await _permissionsService.Can(PermissionKey.Users_EditRoles))
                throw new ActionForbidden();

            var hasRemainingAdmins = await _store
                .WithNoTracking<Data.User>()
                .Where(u => u.Role == UserRoleKey.Admin && u.Id != model.Id)
                .AnyAsync();

            entity.Role = model.Role.Value;
        }

        // everyone can change their sponsor and name
        if (model.SponsorId.NotEmpty())
        {
            // the first "update" to the sponsor (even if it's the same value) knocks off the "Default Sponsor" flag
            entity.HasDefaultSponsor = false;

            // if the sponsor actually changes, note this so we can fix up any player records
            // which have not yet started their session
            if (entity.SponsorId != model.SponsorId)
            {
                entity.SponsorId = model.SponsorId;
                sponsorUpdated = true;
            }
        }

        if (model.PlayAudioOnBrowserNotification is not null)
        {
            entity.PlayAudioOnBrowserNotification = model.PlayAudioOnBrowserNotification.Value;
        }

        await _userStore.Update(entity);
        _localcache.Remove(entity.Id);

        // if the user's sponsor change, update the sponsors of any player records they own which haven't actually
        // started a session (https://github.com/cmu-sei/Gameboard/issues/326)
        if (sponsorUpdated)
        {
            await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.UserId == model.Id)
                .WhereDateIsEmpty(p => p.SessionBegin)
                .ExecuteUpdateAsync(up => up.SetProperty(p => p.SponsorId, model.SponsorId));
        }

        return _mapper.Map<User>(entity);
    }

    public async Task Delete(string id)
    {
        await _store.Delete<Data.User>(id);
        _localcache.Remove(id);
    }

    public async Task<IEnumerable<UserOnly>> List<TProject>(UserSearch model) where TProject : class
    {
        var query = _store
            .WithNoTracking<Data.User>();

        if (model.Term.NotEmpty())
        {
            model.Term = model.Term.ToLower();
            query = query.Where(u =>
                u.Id.StartsWith(model.Term) ||
                u.Name.ToLower().Contains(model.Term) ||
                u.ApprovedName.ToLower().Contains(model.Term)
            );
        }

        if (model.WantsRoles)
            query = query.Where(u => ((int)u.Role) > 0);

        if (model.WantsPending)
            query = query.Where(u => u.NameStatus.Equals(AppConstants.NameStatusPending) && u.Name != u.ApprovedName);

        if (model.WantsDisallowed)
            query = query.Where(u => !string.IsNullOrEmpty(u.NameStatus) && !u.NameStatus.Equals(AppConstants.NameStatusPending));

        if (model.EligibleForGameId.IsNotEmpty())
            query = query.Where(u => !u.Enrollments.Any(p => p.GameId == model.EligibleForGameId && p.Mode == PlayerMode.Competition && p.Game.PlayerMode == PlayerMode.Competition));

        if (model.ExcludeIds.IsNotEmpty())
        {
            var splitIds = model.ExcludeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (splitIds.Length != 0)
                query = query.Where(u => !splitIds.Contains(u.Id));
        }

        var sorted = false;
        if (model.Sort.IsNotEmpty())
        {
            if (model.Sort.Contains("lastLogin", StringComparison.CurrentCultureIgnoreCase))
            {
                query = query.Sort(u => u.LastLoginDate, model.SortDirection);
            }
            else if (model.Sort.Contains("createdOn"))
            {
                query = query.Sort(u => u.CreatedOn, model.SortDirection);
            }
            else
            {
                query.Sort(u => u.ApprovedName, model.SortDirection);
            }

            sorted = true;
        }

        if (!sorted)
        {
            query = query.Sort(u => u.ApprovedName);
        }

        query = query.Skip(model.Skip);

        if (model.Take > 0)
            query = query.Take(model.Take);

        return await query.Select(u => new UserOnly
        {
            Id = u.Id,
            Name = u.Name,
            NameStatus = u.NameStatus,
            ApprovedName = u.ApprovedName,
            CreatedOn = u.CreatedOn,
            LastLoginDate = u.LastLoginDate,
            LoginCount = u.LoginCount,
            Role = u.Role,
            Sponsor = new SponsorWithParentSponsor
            {
                Id = u.SponsorId,
                Name = u.Sponsor.Name,
                Logo = u.Sponsor.Logo,
                ParentSponsor = u.Sponsor.ParentSponsorId == null ? null : new Sponsor
                {
                    Id = u.Sponsor.ParentSponsorId,
                    Name = u.Sponsor.ParentSponsor.Name,
                    Logo = u.Sponsor.ParentSponsor.Logo,
                    ParentSponsorId = u.Sponsor.ParentSponsorId
                }
            }
        })
        .ToArrayAsync();
    }

    public async Task<SimpleEntity[]> ListSupport(SearchFilter model)
    {
        var roles = await _permissionsService.GetRolesWithPermission(PermissionKey.Support_ManageTickets);

        var q = _store.WithNoTracking<Data.User>();
        q = q.Where(u => roles.Contains(u.Role));

        if (model.Term.NotEmpty())
        {
            model.Term = model.Term.ToLower();
            q = q.Where
            (
                u =>
                    u.Id.StartsWith(model.Term) ||
                    u.Name.ToLower().Contains(model.Term) ||
                    u.ApprovedName.ToLower().Contains(model.Term)
            );
        }

        return await _mapper.ProjectTo<SimpleEntity>(q).ToArrayAsync();
    }

    private async Task<User> BuildUserDto(Data.User user)
    {
        var mapped = _mapper.Map<User>(user);
        mapped.RolePermissions = await _permissionsService.GetPermissions(user.Role);
        return mapped;
    }
}
