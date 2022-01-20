// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Services
{
    public class UserService
    {
        IUserStore Store { get; }
        IMapper Mapper { get; }

        private readonly IMemoryCache _localcache;
        private readonly INameService _namesvc;

        public UserService (
            IUserStore store,
            IMapper mapper,
            IMemoryCache cache,
            INameService namesvc
        ){
            Store = store;
            Mapper = mapper;
            _localcache = cache;
            _namesvc = namesvc;
        }

        /// <summary>
        /// If user exists update fields
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<User> GetOrAdd(NewUser model)
        {
            var entity = await Store.Retrieve(model.Id);

            if (entity is Data.User)
            {
                // entity.Name = model.Name;
                // // entity.Email = model.Email;
                // // entity.Username = model.Username;
                // await Store.Update(entity);
            }
            else
            {
                entity = Mapper.Map<Data.User>(model);

                bool found = false;
                int i = 0;
                do {
                    entity.ApprovedName = _namesvc.GetRandomName();
                    entity.Name = entity.ApprovedName;

                    // check uniqueness
                    found = await Store.DbSet.AnyAsync(p =>
                        p.Id != entity.Id &&
                        p.Name == entity.Name
                    );
                } while (found && i++ < 20);

                await Store.Create(entity);
            }

            _localcache.Remove(entity.Id);

            return Mapper.Map<User>(entity);
        }

        public async Task<User> Retrieve(string id)
        {
            return Mapper.Map<User>(await Store.Retrieve(id));
        }

        public async Task Update(ChangedUser model, bool sudo, bool admin = false)
        {
            var entity = await Store.Retrieve(model.Id);

            if (!sudo)
                Mapper.Map(
                    Mapper.Map<SelfChangedUser>(model),
                    entity
                );
            else
            {
                if (!admin && model.Role != entity.Role)
                    throw new ActionForbidden();

                Mapper.Map(model, entity);
            }

            // check uniqueness
            bool found = await Store.DbSet.AnyAsync(p =>
                p.Id != entity.Id &&
                p.Name == entity.Name
            );

            if (found)
                entity.NameStatus = AppConstants.NameStatusNotUnique;
            else if (entity.NameStatus == AppConstants.NameStatusNotUnique)
                entity.NameStatus = "";

            if (entity.Name == entity.ApprovedName)
                entity.NameStatus = "";

            await Store.Update(entity);

            _localcache.Remove(entity.Id);

        }

        public async Task Delete(string id)
        {
            await Store.Delete(id);

            _localcache.Remove(id);

        }

        public async Task<User[]> List(UserSearch model)
        {
            var q = Store.List(model.Term);

            if (model.Term.HasValue())
                q = q.Where(u =>
                    u.Id.StartsWith(model.Term) ||
                    u.Name.ToLower().Contains(model.Term) ||
                    u.ApprovedName.ToLower().Contains(model.Term)
                );

            if (model.WantsRoles)
                q = q.Where(u => ((int)u.Role) > 0);

            if (model.WantsPending)
                q = q.Where(u => string.IsNullOrEmpty(u.NameStatus) && u.Name != u.ApprovedName);

            if (model.WantsDisallowed)
                q = q.Where(u => !string.IsNullOrEmpty(u.NameStatus));

            q = q.OrderBy(p => p.ApprovedName);

            q = q.Skip(model.Skip);

            if (model.Take > 0)
                q = q.Take(model.Take);

            return await Mapper.ProjectTo<User>(q).ToArrayAsync();
        }

    }

}
