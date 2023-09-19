// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Services;

public class UserService
{
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly SponsorService _sponsorService;
    private readonly IStore<Data.User> _userStore;
    private readonly IMemoryCache _localcache;
    private readonly INameService _namesvc;

    public UserService(
        INowService now,
        SponsorService sponsorService,
        IStore<Data.User> userStore,
        IMapper mapper,
        IMemoryCache cache,
        INameService namesvc
    )
    {
        _localcache = cache;
        _mapper = mapper;
        _namesvc = namesvc;
        _now = now;
        _sponsorService = sponsorService;
        _userStore = userStore;
    }

    /// <summary>
    /// If user exists update fields
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<TryCreateUserResult> TryCreate(NewUser model)
    {
        if (model.Id.IsEmpty())
            throw new ArgumentException(nameof(model.Id));

        var entity = await _userStore
            .ListWithNoTracking()
                .Include(u => u.Sponsor)
            .FirstOrDefaultAsync(u => u.Id == model.Id);

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
        if (!await _userStore.AnyAsync())
            entity.Role = AppConstants.AllRoles;

        // record creation date and first login
        if (entity.CreatedOn.DoesntHaveValue())
        {
            entity.CreatedOn = _now.Get();
            entity.LastLoginDate = entity.CreatedOn;
        }

        // assign the user to the default sponsor 
        entity.Sponsor = await _sponsorService.GetDefaultSponsor();
        entity.HasDefaultSponsor = true;

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

    public async Task<User> Update(ChangedUser model, bool sudo, bool admin = false)
    {
        var entity = await _userStore.Retrieve(model.Id);
        bool differentName = entity.Name != model.Name;

        if (!sudo)
        {
            _mapper.Map(
                _mapper.Map<SelfChangedUser>(model),
                entity
            );

            entity.NameStatus = entity.Name != entity.ApprovedName
                ? "pending"
                : ""
            ;
        }
        else
        {
            if (!admin && model.Role != entity.Role)
                throw new ActionForbidden();

            _mapper.Map(model, entity);
        }

        if (differentName)
        {
            // check uniqueness
            bool found = await _userStore.DbSet.AnyAsync(p =>
                p.Id != entity.Id &&
                p.Name == entity.Name
            );

            if (found)
                entity.NameStatus = AppConstants.NameStatusNotUnique;
        }

        await _userStore.Update(entity);
        _localcache.Remove(entity.Id);

        return _mapper.Map<User>(entity);
    }

    public async Task Delete(string id)
    {
        await _userStore.Delete(id);
        _localcache.Remove(id);
    }

    public async Task<IEnumerable<TProject>> List<TProject>(UserSearch model) where TProject : class, IUserViewModel
    {
        var q = _userStore.List(model.Term);

        if (model.Term.NotEmpty())
        {
            model.Term = model.Term.ToLower();
            q = q.Where(u =>
                u.Id.StartsWith(model.Term) ||
                u.Name.ToLower().Contains(model.Term) ||
                u.ApprovedName.ToLower().Contains(model.Term)
            );
        }

        if (model.WantsRoles)
            q = q.Where(u => ((int)u.Role) > 0);

        if (model.WantsPending)
            q = q.Where(u => u.NameStatus.Equals(AppConstants.NameStatusPending));

        if (model.WantsDisallowed)
            q = q.Where(u => !string.IsNullOrEmpty(u.NameStatus) && !u.NameStatus.Equals(AppConstants.NameStatusPending));

        q = q.OrderBy(p => p.ApprovedName);

        q = q.Skip(model.Skip);

        if (model.Take > 0)
            q = q.Take(model.Take);

        return await _mapper
            .ProjectTo<TProject>(q)
            .ToArrayAsync();
    }

    public async Task<UserSimple[]> ListSupport(SearchFilter model)
    {
        var q = _userStore.List(model.Term);

        // Might want to also include observers if they can be assigned. Or just make possible assignees "Support" roles
        q = q.Where(u => u.Role.HasFlag(UserRole.Support));

        if (model.Term.NotEmpty())
        {
            model.Term = model.Term.ToLower();
            q = q.Where(u =>
                u.Id.StartsWith(model.Term) ||
                u.Name.ToLower().Contains(model.Term) ||
                u.ApprovedName.ToLower().Contains(model.Term)
            );
        }

        return await _mapper.ProjectTo<UserSimple>(q).ToArrayAsync();
    }

    internal bool HasRole(User user, UserRole role)
    {
        return user.Role.HasFlag(role);
    }
}
