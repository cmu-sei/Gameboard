// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Data
{

    public class ChallengeSpecStore : Store<ChallengeSpec>, IChallengeSpecStore
    {
        public ChallengeSpecStore(GameboardDbContext dbContext)
        : base(dbContext) { }
    }
}
