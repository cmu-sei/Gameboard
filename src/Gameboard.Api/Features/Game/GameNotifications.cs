using MediatR;

namespace Gameboard.Api.Features.Games;

public record GameEnrolledPlayersChangeContext(string GameId, bool IsSyncStartGame);
public record GameEnrolledPlayersChangeNotification(GameEnrolledPlayersChangeContext Context) : INotification;
