using AutoMapper;

namespace Gameboard.Api.Features.GameEngine;

public class GameEngineMaps : Profile
{
    public GameEngineMaps()
    {
        CreateMap<TopoMojo.Api.Client.GameState, GameState>();
    }
}
