using MediatR;

namespace Gameboard.Api.Features.Games;

public record IsSyncStartReadyQuery(string gameId) : IRequest<SyncStartState>;
