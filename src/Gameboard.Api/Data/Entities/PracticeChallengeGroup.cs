// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace Gameboard.Api.Data;

public class PracticeChallengeGroup : IEntity
{
    public string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required bool IsFeatured { get; set; }
    public string ImageUrl { get; set; }
    public NpgsqlTsVector TextSearchVector { get; set; }

    // many-to-many with challenge specs
    public required ICollection<PracticeChallengeGroupChallengeSpec> ChallengeSpecs { get; set; } = [];

    // we're going to allow nesting to a maximum depth of one (i.e. we have "groups" and "subgroups"),
    // but we're enforcing that with app logic, so we just have a standard self-referring navigation here
    public PracticeChallengeGroup ParentGroup { get; set; }
    public string ParentGroupId { get; set; }
    public ICollection<PracticeChallengeGroup> ChildGroups { get; set; } = [];

    public required DateTimeOffset CreatedOn { get; set; }
    public User CreatedByUser { get; set; }
    public required string CreatedByUserId { get; set; }

    public DateTimeOffset UpdatedOn { get; set; }
    public User UpdatedByUser { get; set; }
    public string UpdatedByUserId { get; set; }
}
