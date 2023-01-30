// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gameboard.Api.Data.Abstractions
{
    public interface IChallengeStore : IStore<Challenge>
    {
        Task ArchiveChallenges(IEnumerable<Api.ArchivedChallenge> challenges);
        Task<Data.Challenge> Load(NewChallenge model);
        Task<Data.Challenge> Load(string id);
        Task UpdateTeam(string teamId);
        Task UpdateEtd(string specId);
        Task<int> ChallengeGamespaceCount(string teamId);
        Task<Data.Challenge> ResolveApiKey(string key);
    }
}
