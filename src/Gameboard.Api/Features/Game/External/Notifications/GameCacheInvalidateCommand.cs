using MediatR;

namespace Gameboard.Api.Features.Games;

public record GameCacheInvalidateCommand(string GameId) : INotification;
