using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Games.External;

public sealed record UpsertExternalGameHost
(
    string Id,
    string Name,
    string ClientUrl,
    string HostUrl,
    string StartupEndpoint,
    bool DestroyResourcesOnDeployFailure,
    int? GamespaceDeployBatchSize = null,
    string HostApiKey = null,
    string PingEndpoint = null,
    string TeamExtendedEndpoint = null
);

public sealed record UpsertExternalGameHostCommand(UpsertExternalGameHost Host) : IRequest<ExternalGameHost>;

internal sealed class UpsertExternalGameHandler : IRequestHandler<UpsertExternalGameHostCommand, ExternalGameHost>
{
    private readonly EntityExistsValidator<UpsertExternalGameHostCommand, ExternalGameHost> _hostExists;
    private readonly IStore _store;
    private readonly IValidatorService<UpsertExternalGameHostCommand> _validatorService;

    public UpsertExternalGameHandler
    (
        EntityExistsValidator<UpsertExternalGameHostCommand, ExternalGameHost> hostExists,
        IStore store,
        IValidatorService<UpsertExternalGameHostCommand> validatorService
    )
    {
        _hostExists = hostExists;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task<ExternalGameHost> Handle(UpsertExternalGameHostCommand request, CancellationToken cancellationToken)
    {
        if (request.Host.Id.IsNotEmpty())
            _validatorService.AddValidator(_hostExists.UseProperty(c => c.Host.Id));

        _validatorService.AddValidator((req, ctx) =>
        {
            if (request.Host.Name.IsEmpty())
                ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.Host.Name), req.Host.Name));

            if (request.Host.ClientUrl.IsEmpty())
                ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.Host.ClientUrl), req.Host.ClientUrl));

            if (request.Host.HostUrl.IsEmpty())
                ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.Host.HostUrl), req.Host.HostUrl));

            if (request.Host.StartupEndpoint.IsEmpty())
                ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.Host.StartupEndpoint), req.Host.StartupEndpoint));
        });

        await _validatorService.Validate(request, cancellationToken);
        var retVal = new ExternalGameHost
        {
            Id = request.Host.Id.IsEmpty() ? null : request.Host.Id,
            Name = request.Host.Name,
            ClientUrl = request.Host.ClientUrl,
            DestroyResourcesOnDeployFailure = request.Host.DestroyResourcesOnDeployFailure,
            GamespaceDeployBatchSize = request.Host.GamespaceDeployBatchSize is null || request.Host.GamespaceDeployBatchSize.Value < 1 ? null : request.Host.GamespaceDeployBatchSize,
            HostApiKey = request.Host.HostApiKey.IsEmpty() ? null : request.Host.HostApiKey,
            HostUrl = request.Host.HostUrl,
            PingEndpoint = request.Host.PingEndpoint.IsEmpty() ? null : request.Host.PingEndpoint,
            StartupEndpoint = request.Host.StartupEndpoint,
            TeamExtendedEndpoint = request.Host.TeamExtendedEndpoint.IsEmpty() ? null : request.Host.TeamExtendedEndpoint
        };

        if (request.Host.Id.IsEmpty())
            retVal = await _store.Create(retVal);
        else
            retVal = await _store.SaveUpdate(retVal, cancellationToken);

        return retVal;
    }
}
