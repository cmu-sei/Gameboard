using System.Linq;
using AutoMapper;
using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Features.GameEngine;

public class GameEngineMaps : Profile
{
    public GameEngineMaps()
    {
        // TODO: injection, but it's hard
        var jsonService = JsonService.WithGameboardSerializerOptions();

        // api-level maps
        CreateMap<Api.Data.Player, GameEnginePlayer>()
            .ForMember(gep => gep.SubjectId, o => o.MapFrom(p => p.Id))
            .ForMember(gep => gep.SubjectName, o => o.MapFrom(p => p.ApprovedName))
            .ForMember(gep => gep.GamespaceId, o => o.Ignore())
            .ForMember(gep => gep.Permission, o => o.Ignore());

        // engine-level maps
        CreateMap<GameEnginePlayer, Api.Data.Player>(MemberList.Source)
            .ForMember(p => p.Id, o => o.MapFrom(gep => gep.SubjectId))
            .ForMember(p => p.ApprovedName, o => o.MapFrom(gep => gep.SubjectName))
            .ForMember(p => p.Role, o => o.MapFrom(gep => gep.IsManager ? PlayerRole.Manager : PlayerRole.Member))
            // we are mapping manager (see above), but we need to tell automapper we've got it under control
            .ForSourceMember(gep => gep.IsManager, o => o.DoNotValidate())
            .ForSourceMember(gep => gep.Permission, o => o.DoNotValidate())
            .ForSourceMember(gep => gep.GamespaceId, o => o.DoNotValidate());

        CreateMap<GameEngineGameState, Api.Data.Challenge>()
            .ForMember(c => c.EndTime, o => o.MapFrom(s => s.ExpirationTime))
            .ForMember(c => c.HasDeployedGamespace, o => o.MapFrom(s => s.HasDeployedGamespace))
            .ForMember(c => c.LastScoreTime, o => o.MapFrom(s => s.Challenge.LastScoreTime))
            .ForMember(c => c.Id, o => o.MapFrom(s => s.Id))
            .ForMember(c => c.PlayerId, o => o.MapFrom(p => p.Players.Where(p => p.IsManager).First().SubjectId))
            .ForMember(c => c.Points, o => o.MapFrom(s => s.Challenge.MaxPoints))
            .ForMember(c => c.Score, o => o.MapFrom(s => s.Challenge.Score))
            .ForMember(c => c.State, o => o.MapFrom(s => jsonService.Serialize(s)))
            .ForMember(c => c.ExternalId, o => o.MapFrom(s => s.Id))
            // ignore entity properties because we don't want EF to think that we're trying to insert new ones
            .ForMember(c => c.Player, o => o.Ignore())
            // game engine type will need to be resolved using an aftermap expression during mapping
            .ForMember(c => c.GameEngineType, o => o.Ignore())
            // similarly, engines don't know about things like games, tickets, and bonuses
            .ForMember(c => c.Events, o => o.Ignore())
            .ForMember(c => c.Feedback, o => o.Ignore())
            .ForMember(c => c.GameId, o => o.Ignore())
            .ForMember(c => c.Game, o => o.Ignore())
            .ForMember(c => c.GraderKey, o => o.Ignore())
            .ForMember(c => c.LastSyncTime, o => o.Ignore())
            .ForMember(c => c.SpecId, o => o.Ignore())
            .ForMember(c => c.Tag, o => o.Ignore())
            .ForMember(c => c.TeamId, o => o.Ignore())
            .ForMember(c => c.Tickets, o => o.Ignore())
            .ForMember(c => c.AwardedBonuses, o => o.Ignore())
            .ForMember(c => c.AwardedManualBonuses, o => o.Ignore());

        // engine: topo
        CreateMap<TopoMojo.Api.Client.AnswerSubmission, GameEngineAnswerSubmission>();
        CreateMap<TopoMojo.Api.Client.GameState, GameEngineGameState>();
        CreateMap<TopoMojo.Api.Client.Permission, GameEnginePlayerPermission>();
        CreateMap<TopoMojo.Api.Client.Player, GameEnginePlayer>();
        CreateMap<TopoMojo.Api.Client.QuestionView, GameEngineQuestionView>();
        CreateMap<TopoMojo.Api.Client.VmState, GameEngineVmState>();
        CreateMap<TopoMojo.Api.Client.ChallengeView, GameEngineChallengeView>();
        CreateMap<TopoMojo.Api.Client.SectionSubmission, GameEngineSectionSubmission>();
        CreateMap<GameEngineAnswerSubmission, TopoMojo.Api.Client.AnswerSubmission>();
        CreateMap<GameEngineSectionSubmission, TopoMojo.Api.Client.SectionSubmission>();
    }
}
