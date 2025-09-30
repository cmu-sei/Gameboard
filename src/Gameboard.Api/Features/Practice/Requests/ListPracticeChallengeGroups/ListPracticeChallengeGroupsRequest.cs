// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Features.Practice;

public sealed class ListPracticeChallengeGroupsRequest
{
    public string ContainChallengeSpecId { get; set; }
    public bool GetRootOnly { get; set; }
    public string ParentGroupId { get; set; }
    public string SearchTerm { get; set; }
}
