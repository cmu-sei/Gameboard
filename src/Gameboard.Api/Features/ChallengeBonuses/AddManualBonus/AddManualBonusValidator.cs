// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Features.ChallengeBonuses;

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

internal class AddManualBonusValidator : IGameboardRequestValidator<AddManualBonusCommand>
{
    private readonly EntityExistsValidator<AddManualBonusCommand, Data.Challenge> _challengeExists;
    private readonly TeamExistsValidator<AddManualBonusCommand> _teamExists;
    private readonly IValidatorService<AddManualBonusCommand> _validatorService;

    public AddManualBonusValidator
    (
        EntityExistsValidator<AddManualBonusCommand, Data.Challenge> challengeExists,
        TeamExistsValidator<AddManualBonusCommand> teamExists,
        IValidatorService<AddManualBonusCommand> validatorService
    )
    {
        _challengeExists = challengeExists;
        _teamExists = teamExists;
        _validatorService = validatorService;
    }

    public async Task Validate(AddManualBonusCommand request, CancellationToken cancellationToken)
    {
        _validatorService
            .Auth(c => c.Require(PermissionKey.Scores_AwardManualBonuses))
            .AddValidator((req, context) =>
            {
                if ((req.ChallengeId.IsEmpty() && req.TeamId.IsEmpty()) || (req.ChallengeId.IsNotEmpty() && req.TeamId.IsNotEmpty()))
                    context.AddValidationException(new InvalidManualBonusConfiguration(req.ChallengeId, req.TeamId));
            });

        _validatorService.AddValidator((request, context) =>
        {
            if (request.Model.PointValue <= 0)
                context.AddValidationException(new InvalidParameterValue<double>(nameof(request.Model.PointValue), "Must be greater than zero.", request.Model.PointValue));
        });

        _validatorService.AddValidator((request, context) =>
        {
            if (string.IsNullOrWhiteSpace(request.Model.Description))
                context.AddValidationException(new MissingRequiredInput<string>(nameof(request.Model.Description), request.Model.Description));
        });

        if (request.ChallengeId.IsNotEmpty())
            _validatorService.AddValidator(_challengeExists.UseProperty(r => r.ChallengeId));
        else
            _validatorService.AddValidator(_teamExists.UseProperty(r => r.TeamId));

        await _validatorService.Validate(request, cancellationToken);
    }
}
