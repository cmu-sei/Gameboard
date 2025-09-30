// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gameboard.Api.Data;

public class SupportSettings : IEntity
{
    public string Id { get; set; }
    public ICollection<SupportSettingsAutoTag> AutoTags { get; set; } = [];
    public string SupportPageGreeting { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }

    public string UpdatedByUserId;
    public Data.User UpdatedByUser;
}
