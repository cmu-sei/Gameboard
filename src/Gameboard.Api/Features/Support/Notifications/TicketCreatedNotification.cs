// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using MediatR;

namespace Gameboard.Api.Features.Support;

public sealed class TicketCreatedNotification : INotification
{
    public required int Key { get; set; }
    public required string FullKey { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required SimpleEntity Creator { get; set; }

    public SimpleEntity Challenge { get; set; }
}
