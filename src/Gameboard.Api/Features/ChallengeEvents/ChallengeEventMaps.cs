using AutoMapper;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.ChallengeEvents;

public class ChallengeEventMaps : Profile
{
    public ChallengeEventMaps()
    {
        CreateMap<ChallengeEvent, ChallengeEventSummary>();
    }
}