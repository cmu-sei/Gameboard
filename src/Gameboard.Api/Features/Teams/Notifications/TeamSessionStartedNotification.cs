// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using MediatR;

namespace Gameboard.Api.Features.Teams;

public sealed record TeamSessionStartedNotification(string TeamId, CalculatedSessionWindow Session) : INotification;
