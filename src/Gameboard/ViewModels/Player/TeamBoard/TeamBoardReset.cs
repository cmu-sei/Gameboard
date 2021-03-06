// Copyright 2020 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.ValidationRules;
using Stack.Validation.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gameboard.ViewModels
{
    [Validation(
        typeof(UserCanEditTeam),
        typeof(BoardResetIsAllowed),
        typeof(BoardHasNotEnded),
        typeof(TeamHasTeamBoard)
    )]
    public class TeamBoardReset
    {
        public string BoardId { get; set; }

        public string TeamId { get; set; }
    }
}

