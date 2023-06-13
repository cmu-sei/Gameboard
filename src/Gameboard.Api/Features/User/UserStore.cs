// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Data;

public class UserStore : Store<User>, IUserStore
{
    public UserStore(GameboardDbContext dbContext, IGuidService guids)
        : base(dbContext, guids) { }

    public override Task<User> Create(User entity)
    {
        // first user gets admin
        if (DbSet.Any().Equals(false))
            entity.Role = AppConstants.AllRoles;

        return base.Create(entity);
    }
}
