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

public class UserService(
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

        var entity = await _userStore
            .ListWithNoTracking()
                .Include(u => u.Sponsor)
            .SingleOrDefaultAsync(u => u.Id == model.Id);

        if (entity is not null)
        {
            return new TryCreateUserResult
            {
                IsNewUser = false,
                User = _mapper.Map<User>(entity)
            };
        }

        entity = _mapper.Map<Data.User>(model);

        // first user gets admin
        if (!await _userStore.AnyAsync(u => u.Id != model.Id))
            entity.Role = UserRole.Admin;

        // record creation date and first login
        if (entity.CreatedOn.DoesntHaveValue())
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
        int i = 0;
        do
        {
            entity.ApprovedName = _namesvc.GetRandomName();
            entity.Name = entity.ApprovedName;

            // check uniqueness
            found = await _userStore.AnyAsync(p => p.Id != entity.Id && p.Name == entity.Name);
        } while (found && i++ < 20);

        await _userStore.Create(entity);

        _localcache.Remove(entity.Id);
        return new TryCreateUserResult
        {
            IsNewUser = true,
            User = _mapper.Map<User>(entity)
        };
    }

    public async Task<User> Retrieve(string id)
    {
        return _mapper.Map<User>(await _userStore.Retrieve(id));
    }

    public async Task<User> Update(ChangedUser model, bool canAdminUsers)
    {
        var entity = await _userStore.Retrieve(model.Id, q => q.Include(u => u.Sponsor));
        var sponsorUpdated = false;

        // with user stuff, there are super-users (sudoers) and admins
        // only admins can alter the roles of users
        if (model.Role.HasValue && model.Role != entity.Role)
        {
            if (!await _permissionsService.Can(PermissionKey.Users_EditRoles))
                throw new ActionForbidden();
            else
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

        // if we're editing the (not-approved) name...
        if (model.Name.NotEmpty() && entity.Name != model.Name)
        {
            entity.Name = model.Name.Trim();

            // sudoers change names without the "pending" step
            entity.NameStatus = canAdminUsers ? entity.NameStatus : AppConstants.NameStatusPending;
            // and they automatically copy the requested name to the approved name
            entity.ApprovedName = canAdminUsers ? entity.Name : entity.ApprovedName;

            // after shuffling the name, approved name, and status, check to ensure
            // that the name and approved name are different. if they're the same,
            // they're not in pending status (because the name is approved)
            if (entity.Name == entity.ApprovedName && entity.NameStatus == AppConstants.NameStatusPending)
                entity.NameStatus = string.Empty;

            // if the name is in use, change the namestatus to reflect this fact
            // check uniqueness
            var found = await _userStore.DbSet.AnyAsync(p =>
                p.Id != entity.Id &&
                p.Name == entity.Name
            );

            if (found)
                entity.NameStatus = AppConstants.NameStatusNotUnique;
        }

        // only sudoers can approve names
        if (canAdminUsers && model.ApprovedName.NotEmpty())
        {
            entity.ApprovedName = model.ApprovedName;
            entity.NameStatus = null;
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
        await _userStore.Delete(id);
        _localcache.Remove(id);
    }

    public async Task<IEnumerable<UserOnly>> List<TProject>(UserSearch model) where TProject : class
    {
        var q = _userStore
            .List(model.Term)
            .AsNoTracking()
            .Include(u => u.Sponsor).Where(u => true);

        if (model.Term.NotEmpty())
        {
            model.Term = model.Term.ToLower();
            q = q.Where(u =>
                u.Id.StartsWith(model.Term) ||
                u.Name.Contains(model.Term, StringComparison.CurrentCultureIgnoreCase) ||
                u.ApprovedName.Contains(model.Term, StringComparison.CurrentCultureIgnoreCase)
            );
        }

        if (model.WantsRoles)
            q = q.Where(u => ((int)u.Role) > 0);

        if (model.WantsPending)
            q = q.Where(u => u.NameStatus.Equals(AppConstants.NameStatusPending) && u.Name != u.ApprovedName);

        if (model.WantsDisallowed)
            q = q.Where(u => !string.IsNullOrEmpty(u.NameStatus) && !u.NameStatus.Equals(AppConstants.NameStatusPending));

        if (model.EligibleForGameId.IsNotEmpty())
            q = q.Where(u => !u.Enrollments.Any(p => p.GameId == model.EligibleForGameId && p.Mode == PlayerMode.Competition && p.Game.PlayerMode == PlayerMode.Competition));

        if (model.ExcludeIds.IsNotEmpty())
        {
            var splitIds = model.ExcludeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (splitIds.Length != 0)
                q = q.Where(u => !splitIds.Contains(u.Id));
        }

        q = q.OrderBy(p => p.ApprovedName);
        q = q.Skip(model.Skip);

        if (model.Take > 0)
            q = q.Take(model.Take);

        return await _mapper.ProjectTo<UserOnly>(q).ToArrayAsync();
    }

    public async Task<UserSimple[]> ListSupport(SearchFilter model)
    {
        var q = _userStore.List(model.Term);

        var roles = await _permissionsService.GetRolesWithPermission(PermissionKey.Support_ManageTickets);
        q = q.Where(u => roles.Contains(u.Role));

        if (model.Term.NotEmpty())
        {
            model.Term = model.Term.ToLower();
            q = q.Where
            (
                u =>
                    u.Id.StartsWith(model.Term) ||
                    u.Name.Contains(model.Term, StringComparison.CurrentCultureIgnoreCase) ||
                    u.ApprovedName.Contains(model.Term, StringComparison.CurrentCultureIgnoreCase)
            );
        }

        return await _mapper.ProjectTo<UserSimple>(q).ToArrayAsync();
    }
}
