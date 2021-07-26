// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Gameboard.Api.Data
{
    public class Sponsor : IEntity
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Logo { get; set; }

        public bool Approved { get; set; }
    }
}
