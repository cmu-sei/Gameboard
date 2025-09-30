// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.


namespace Gameboard.Api.Common;

public static class UserExtensions
{
    public static SimpleEntity ToSimpleEntity(this User user)
        => new() { Id = user.Id, Name = user.ApprovedName };
}
