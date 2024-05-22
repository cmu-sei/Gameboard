using MediatR;

namespace Gameboard.Api.Features.Teams;

public sealed record TeamSessionStartedNotification(string TeamId, CalculatedSessionWindow Session) : INotification;
