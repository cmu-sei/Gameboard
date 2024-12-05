using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Feedback;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetFeedbackGameReportQuery(string GameId, string ChallengeSpecId) : IRequest<FeedbackGameReportResults>, IReportQuery;

internal sealed class GetFeedbackGameReportHandler(
    FeedbackService feedbackService,
    EntityExistsValidator<GetFeedbackGameReportQuery, Data.Game> gameExists,
    IGameService gameService,
    ReportsQueryValidator reportsQueryValidator,
    EntityExistsValidator<GetFeedbackGameReportQuery, Data.ChallengeSpec> specExists,
    IValidatorService<GetFeedbackGameReportQuery> validatorService
    ) : IRequestHandler<GetFeedbackGameReportQuery, FeedbackGameReportResults>
{
    private readonly FeedbackService _feedbackService = feedbackService;
    private readonly EntityExistsValidator<GetFeedbackGameReportQuery, Data.Game> _gameExists = gameExists;
    private readonly IGameService _gameService = gameService;
    private readonly ReportsQueryValidator _reportsQueryValidator = reportsQueryValidator;
    private readonly EntityExistsValidator<GetFeedbackGameReportQuery, Data.ChallengeSpec> _specExists = specExists;
    private readonly IValidatorService<GetFeedbackGameReportQuery> _validatorService = validatorService;

    public async Task<FeedbackGameReportResults> Handle(GetFeedbackGameReportQuery request, CancellationToken cancellationToken)
    {
        // validate
        await _reportsQueryValidator.Validate(request, cancellationToken);
        _validatorService.AddValidator(_gameExists.UseProperty(r => r.GameId));

        if (request.ChallengeSpecId.IsNotEmpty())
            _validatorService.AddValidator(_specExists.UseProperty(r => r.ChallengeSpecId));

        await _validatorService.Validate(request, cancellationToken);

        // gameId must be specified, even for challenge feedback, since template is stored in Game
        var game = await _gameService.Retrieve(request.GameId);
        var gameSimple = new SimpleEntity { Id = game.Id, Name = game.Name };

        if (game.FeedbackTemplate is null)
            return new()
            {
                Game = gameSimple,
                Questions = []
            };

        var query = new FeedbackSearchParams
        {
            GameId = request.GameId,
            Sort = string.Empty,
            SubmitStatus = string.Empty,
            Type = FeedbackSearchParams.GameType
        };
        var feedback = await _feedbackService.ListFull(query);

        var questionTemplate = _feedbackService.GetTemplate(query.WantsGame, game);
        if (questionTemplate == null)
        {
            return new() { Game = gameSimple, Questions = Array.Empty<FeedbackQuestion>() };
        }
        var submittedFeedback = feedback.Where(f => f.Submitted).ToArray();
        var expandedTable = _feedbackService.MakeHelperList(submittedFeedback);
        var maxResponses = await _feedbackService.GetFeedbackMaxResponses(query);
        var questionStats = _feedbackService.GetFeedbackQuestionStats(questionTemplate, expandedTable);

        return new()
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Questions = game.FeedbackTemplate.Game,
            Stats = new FeedbackStats
            {
                ChallengeSpecId = request.ChallengeSpecId,
                ConfiguredCount = questionTemplate.Length,
                LikertCount = questionTemplate.Where(q => q.Type == "likert").Count(),
                TextCount = questionTemplate.Where(q => q.Type == "text").Count(),
                SelectOneCount = questionTemplate.Where(q => q.Type == "selectOne").Count(),
                SelectManyCount = questionTemplate.Where(q => q.Type == "selectMany").Count(),
                RequiredCount = questionTemplate.Where(q => q.Required).Count(),
                ResponsesCount = feedback.Length,
                MaxResponseCount = maxResponses,
                InProgressCount = feedback.Length - submittedFeedback.Length,
                SubmittedCount = submittedFeedback.Length,
                QuestionStats = questionStats
            }
        };
    }
}
