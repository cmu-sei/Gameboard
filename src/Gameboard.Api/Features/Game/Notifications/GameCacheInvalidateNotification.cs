using MediatR;

namespace Gameboard.Api.Features.Games;

public record GameCacheInvalidateNotification(string GameId) : INotification;
