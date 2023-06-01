// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data.Abstractions
{
    public interface IChallengeSpecStore : IStore<Data.ChallengeSpec> { }
}


namespace Gameboard.Api.Data
{
    internal class ChallengeSpecStore : Store<Data.ChallengeSpec>, IChallengeSpecStore
    {
        public ChallengeSpecStore(GameboardDbContext dbContext, IGuidService guids)
        : base(dbContext, guids) { }
    }
}
