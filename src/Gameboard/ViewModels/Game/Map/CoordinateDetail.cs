// Copyright 2020 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.


using Gameboard.Data;
using System.Collections.Generic;

namespace Gameboard.ViewModels
{    

    public class CoordinateDetail : IChallengeModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Radius { get; set; }

        public int Points { get; set; }

        public ActionType ActionType { get; set; }

        public string ActionValue { get; set; }

        public ChallengeLinkDetail ChallengeLink { get; set; } = new ChallengeLinkDetail();

        public ChallengeDetail Challenge { get; set; }

        public bool IsDisabled { get; set; }
    }
}

