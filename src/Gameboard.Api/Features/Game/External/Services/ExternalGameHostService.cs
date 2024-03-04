using System;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameHostService
{
    Task ExtendTeam(string teamId, DateTimeOffset newSessionEnd);
    Task StartGame(ExternalGameStartMetaData metaData);
}

internal class GamebrainGameHostService : IExternalGameHostService
{
    public async Task ExtendTeam(string teamId, DateTimeOffset newSessionEnd)
    {
        throw new NotImplementedException();
    }

    public Task StartGame(ExternalGameStartMetaData metaData)
    {
        throw new NotImplementedException();
    }
}
