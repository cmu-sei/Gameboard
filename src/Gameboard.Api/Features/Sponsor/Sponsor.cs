// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Gameboard.Api;

public class Sponsor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Logo { get; set; }
    public required string ParentSponsorId { get; set; }
}

public class SponsorWithParentSponsor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Logo { get; set; }
    public required Sponsor ParentSponsor { get; set; }
}

public class SponsorWithChildSponsors
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Logo { get; set; }
    public required IEnumerable<Sponsor> ChildSponsors { get; set; }
}

public class NewSponsor
{
    public string Name { get; set; }
    public string ParentSponsorId { get; set; }
}

public class UpdateSponsorRequest
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ParentSponsorId { get; set; }
}

public class SponsorSearch
{
    public string ExcludeSponsorId { get; set; }
    public bool? HasParent { get; set; }
}
