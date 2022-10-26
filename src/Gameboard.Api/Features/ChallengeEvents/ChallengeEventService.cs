
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.ChallengeEvents;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Services;

public class ChallengeEventService : _Service
{
    private ITopoMojoApiClient Mojo { get; }
    private IChallengeEventStore Store { get; }

    public ChallengeEventService(
        ILogger<ChallengeEventService> logger,
        IMapper mapper,
        CoreOptions options,
        IChallengeEventStore store,
        ITopoMojoApiClient mojo
    ) : base(logger, mapper, options)
    {
        Store = store;
        Mojo = mojo;
    }

    public async Task<ChallengeEvent> Add(NewChallengeEvent model)
    {
        var challengeEvent = Mapper.Map<ChallengeEvent>(model);
        return await Store.Create(challengeEvent);
    }
}