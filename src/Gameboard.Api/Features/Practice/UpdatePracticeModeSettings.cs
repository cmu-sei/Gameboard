// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Practice;

internal class PracticeModeSettingsInvalid : GameboardValidationException
{
    public PracticeModeSettingsInvalid(string settingName, string settingValue, string description)
        : base($"""Practice Area setting "{settingName}" with value "{settingValue}" is invalid. {description}""") { }
}

internal class UpdatePracticeModeSettingsValidator
(
    EntityExistsValidator<UpdatePracticeModeSettingsCommand, Data.User> userExists,
    IValidatorService<UpdatePracticeModeSettingsCommand> validatorService
) : IGameboardRequestValidator<UpdatePracticeModeSettingsCommand>
{
    public Task Validate(UpdatePracticeModeSettingsCommand request, CancellationToken cancellationToken)
    {
        return validatorService
            .Auth(a => a.Require(PermissionKey.Practice_EditSettings))
            .AddValidator((request, context) =>
            {
                if (request.Settings.MaxConcurrentPracticeSessions.HasValue && request.Settings.MaxConcurrentPracticeSessions < 0)
                    context.AddValidationException(new PracticeModeSettingsInvalid(nameof(PracticeModeSettings.MaxConcurrentPracticeSessions), request.Settings.MaxConcurrentPracticeSessions.Value.ToString(), "Max concurrent practice sessions must be either null or non-negative."));

                return Task.CompletedTask;
            }).AddValidator((request, context) =>
            {
                if (request.Settings.MaxPracticeSessionLengthMinutes.HasValue && request.Settings.MaxPracticeSessionLengthMinutes <= 0)
                    context.AddValidationException(new PracticeModeSettingsInvalid(nameof(PracticeModeSettings.MaxPracticeSessionLengthMinutes), request.Settings.MaxPracticeSessionLengthMinutes.Value.ToString(), "Max practice session length must be either null or positive."));

                return Task.CompletedTask;
            })
            .AddValidator(userExists.UseProperty(r => r.ActingUser.Id))
            .Validate(request, cancellationToken);
    }
}

public record UpdatePracticeModeSettingsCommand(PracticeModeSettingsApiModel Settings, User ActingUser) : IRequest;

internal class UpdatePracticeModeSettingsHandler
(
    CoreOptions coreOptions,
    IMapper mapper,
    INowService now,
    IPracticeService practiceService,
    IStore store,
    IGameboardRequestValidator<UpdatePracticeModeSettingsCommand> requestValidator
) : IRequestHandler<UpdatePracticeModeSettingsCommand>
{
    private readonly CoreOptions _coreOptions = coreOptions;
    private readonly IMapper _mapper = mapper;
    private readonly INowService _now = now;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly IStore _store = store;
    private readonly IGameboardRequestValidator<UpdatePracticeModeSettingsCommand> _requestValidator = requestValidator;

    public async Task Handle(UpdatePracticeModeSettingsCommand request, CancellationToken cancellationToken)
    {
        await _requestValidator.Validate(request, cancellationToken);

        var currentSettings = await _practiceService.GetSettings(cancellationToken);
        var updatedSettings = _mapper.Map<PracticeModeSettings>(request.Settings);

        updatedSettings.AttemptLimit = request.Settings.AttemptLimit;
        updatedSettings.CertificateTemplateId = request.Settings.CertificateTemplateId;
        updatedSettings.SuggestedSearches = _practiceService.EscapeSuggestedSearches(request.Settings.SuggestedSearches);
        updatedSettings.Id = currentSettings.Id;
        updatedSettings.UpdatedOn = _now.Get();
        updatedSettings.UpdatedByUserId = request.ActingUser.Id;

        // force a value for default session length, becaues it's required
        if (updatedSettings.DefaultPracticeSessionLengthMinutes <= 0)
        {
            updatedSettings.DefaultPracticeSessionLengthMinutes = _coreOptions.PracticeDefaultSessionLength;
        }

        await _store.SaveUpdate(updatedSettings, cancellationToken);
    }
}
