using MediatR;

namespace Gameboard.Api.Features.Games;

public record GameEnrolledPlayersChangeNotification(string GameId) : INotification;
