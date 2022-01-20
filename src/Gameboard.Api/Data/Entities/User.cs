// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gameboard.Api.Data
{
    public class User : IEntity
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string NameStatus { get; set; }
        public string ApprovedName { get; set; }
        public string Sponsor { get; set; }
        public UserRole Role { get; set; }
        public ICollection<Player> Enrollments { get; set; } = new List<Player>();
    }

}
