// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Data;

internal class ChallengeGateStore : Store<ChallengeGate>, IChallengeGateStore
{
    public ChallengeGateStore(GameboardDbContext dbContext, IGuidService guids)
    : base(dbContext, guids)
    {

    }

    // If entity has searchable fields, use this:
    // public override IQueryable<ChallengeGate> List(string term = null)
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
