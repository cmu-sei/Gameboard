using System.Linq;
using AutoMapper;

namespace Gameboard.Api.Features.GameEngine;

public class GameEngineMaps : Profile
{
    public GameEngineMaps()
    {
        CreateMap<TopoMojo.Api.Client.AnswerSubmission, IGameEngineAnswerSubmission>()
            .ConstructUsing(c => new GameEngineAnswerSubmission());
        CreateMap<TopoMojo.Api.Client.Permission, GameEnginePlayerPermission>();
        CreateMap<TopoMojo.Api.Client.Player, IGameEnginePlayer>()
            .ConstructUsing(c => new GameEnginePlayer());
        CreateMap<TopoMojo.Api.Client.QuestionView, IGameEngineQuestionView>()
            .ConstructUsing(c => new GameEngineQuestionView());
        CreateMap<TopoMojo.Api.Client.VmState, IGameEngineVmState>()
            .ConstructUsing(c => new GameEngineVmState());
        CreateMap<TopoMojo.Api.Client.ChallengeView, IGameEngineChallengeView>()
            .ConstructUsing(c => new GameEngineChallengeView());
        CreateMap<TopoMojo.Api.Client.GameState, IGameEngineGameState>()
            .ConstructUsing(c => new GameEngineGameState());
        CreateMap<TopoMojo.Api.Client.SectionSubmission, IGameEngineSectionSubmission>()
            .ConstructUsing(c => new GameEngineSectionSubmission());
    }
}
