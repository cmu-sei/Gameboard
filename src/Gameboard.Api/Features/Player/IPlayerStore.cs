// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;

namespace Gameboard.Api.Data.Abstractions
{

    public interface IPlayerStore : IStore<Player>
    {
        Task<Player[]> ListTeam(string id);
        Task<Player[]> ListTeamByPlayer(string id);
        Task<Challenge[]> ListTeamChallenges(string id);
        Task<User> GetUserEnrollments(string id);
        Task<Player> LoadBoard(string id);
    }

}
