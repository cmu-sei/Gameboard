// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Data
{

    public class UserStore: Store<User>, IUserStore
    {
        public UserStore(GameboardDbContext dbContext)
        :base(dbContext)
        {

        }

        public override Task<User> Create(User entity)
        {
            // first user gets admin
            if (DbSet.Any().Equals(false))
                entity.Role = AppConstants.AllRoles;

            return base.Create(entity);
        }

        // If entity has searchable fields, use this:
        // public override IQueryable<User> List(string term = null)
        // {
        //     var q = base.List();

        //     if (!string.IsNullOrEmpty(term))
        //     {
        //         term = term.ToLower();

        //         q = q.Where(t =>
        //             t.Name.ToLower().Contains(term)
        //         );
        //     }

        //     return q;
        // }

    }
}
