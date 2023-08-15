using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Practice;

internal class PracticeModeSettingsInvalid : GameboardValidationException
{
    public PracticeModeSettingsInvalid(string settingName, string settingValue, string description)
        : base($"""Practice mode setting "{settingName}" with value "{settingValue}" is invalid. {description}""") { }
}

internal class UpdatePracticeModeSettingsValidator : IGameboardRequestValidator<UpdatePracticeModeSettingsCommand>
{
    private readonly UserRoleAuthorizer _roleAuthorizer;
    private readonly EntityExistsValidator<UpdatePracticeModeSettingsCommand, Data.User> _userExists;
    private readonly IValidatorService<UpdatePracticeModeSettingsCommand> _validatorService;

    public UpdatePracticeModeSettingsValidator
    (
        UserRoleAuthorizer roleAuthorizer,
        EntityExistsValidator<UpdatePracticeModeSettingsCommand, Data.User> userExists,
        IValidatorService<UpdatePracticeModeSettingsCommand> validatorService
    )
    {
        _roleAuthorizer = roleAuthorizer;
        _userExists = userExists;
        _validatorService = validatorService;
    }

    public async Task Validate(UpdatePracticeModeSettingsCommand request)
    {
        _roleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin };
        _roleAuthorizer.Authorize();

        _validatorService.AddValidator((request, context) =>
        {
            if (request.Settings.MaxConcurrentPracticeSessions.HasValue && request.Settings.MaxConcurrentPracticeSessions <= 0)
                context.AddValidationException(new PracticeModeSettingsInvalid(nameof(PracticeModeSettings.MaxConcurrentPracticeSessions), request.Settings.MaxConcurrentPracticeSessions.Value.ToString(), "Max concurrent practice sessions must be either null or non-negative."));

            return Task.CompletedTask;
        });

        _validatorService.AddValidator((request, context) =>
        {
            if (request.Settings.MaxPracticeSessionLengthMinutes.HasValue && request.Settings.MaxPracticeSessionLengthMinutes <= 0)
                context.AddValidationException(new PracticeModeSettingsInvalid(nameof(PracticeModeSettings.MaxPracticeSessionLengthMinutes), request.Settings.MaxPracticeSessionLengthMinutes.Value.ToString(), "Max practice session length must be either null or non-negative."));

            return Task.CompletedTask;
        });


        _validatorService.AddValidator(_userExists.UseProperty(r => r.ActingUser.Id));

        await _validatorService.Validate(request);
    }
}

public record UpdatePracticeModeSettingsCommand(UpdatePracticeModeSettings Settings, User ActingUser) : IRequest;

internal class UpdatePracticeModeSettingsHandler : IRequestHandler<UpdatePracticeModeSettingsCommand>
{
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly UpdatePracticeModeSettingsValidator _validator;

    public UpdatePracticeModeSettingsHandler
    (
        IMapper mapper,
        INowService now,
        IStore store,
        UpdatePracticeModeSettingsValidator validator
    )
    {
        _mapper = mapper;
        _now = now;
        _store = store;
        _validator = validator;
    }

    public async Task Handle(UpdatePracticeModeSettingsCommand request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request);

        var currentSettings = await _store.FirstOrDefaultAsync<PracticeModeSettings>(cancellationToken);
        var updatedSettings = _mapper.Map<Data.PracticeModeSettings>(request.Settings);
        updatedSettings.Id = currentSettings.Id;
        updatedSettings.UpdatedOn = _now.Get();

        await _store.Update(updatedSettings, cancellationToken);
    }
}
