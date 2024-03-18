using MediatR;

namespace Gameboard.Api.Features.Teams;

public record UserJoinedTeamNotification(string UserId, string TeamId) : INotification;
