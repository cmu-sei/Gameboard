// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api
{
    public class SearchFilter
    {
        public string Term { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public string Sort { get; set; }
        public string[] Filter { get; set; } = new string[] {};
    }

    public class Entity {
        public string Id { get; set; }
    }
}
