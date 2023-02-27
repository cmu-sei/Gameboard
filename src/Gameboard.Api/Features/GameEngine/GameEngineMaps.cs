using System.Linq;
using AutoMapper;

namespace Gameboard.Api.Features.GameEngine;

public class GameEngineMaps : Profile
{
    public GameEngineMaps()
    {
        CreateMap<TopoMojo.Api.Client.AnswerSubmission, GameEngineAnswerSubmission>();
        CreateMap<TopoMojo.Api.Client.Permission, GameEnginePlayerPermission>();
        CreateMap<TopoMojo.Api.Client.Player, GameEnginePlayer>();
        CreateMap<TopoMojo.Api.Client.QuestionView, GameEngineQuestionView>();
        CreateMap<TopoMojo.Api.Client.VmState, GameEngineVmState>();
        CreateMap<TopoMojo.Api.Client.ChallengeView, GameEngineChallengeView>();
        CreateMap<TopoMojo.Api.Client.GameState, GameEngineGameState>();
        CreateMap<TopoMojo.Api.Client.SectionSubmission, GameEngineSectionSubmission>();
    }
}
