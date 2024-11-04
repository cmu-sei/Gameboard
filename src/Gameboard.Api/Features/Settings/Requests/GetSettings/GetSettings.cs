using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.App;

public record GetSettingsQuery() : IRequest<GetSettingsResponse>;

internal class GetSettingsHandler(CoreOptions coreOptions, IValidatorService validatorService) : IRequestHandler<GetSettingsQuery, GetSettingsResponse>
{
    private readonly CoreOptions _coreOptions = coreOptions;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<GetSettingsResponse> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(config => config.RequireAuthentication())
            .Validate(cancellationToken);

        return new GetSettingsResponse
        {
            Settings = new PublicSettings
            {
                NameChangeIsEnabled = _coreOptions.NameChangeIsEnabled
            }
        };
    }
}

