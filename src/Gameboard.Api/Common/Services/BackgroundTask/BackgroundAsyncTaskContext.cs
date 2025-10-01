// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Common.Services;

public sealed class BackgroundAsyncTaskContext
{
    public User ActingUser { get; set; }
    public string AppBaseUrl { get; set; }
}
