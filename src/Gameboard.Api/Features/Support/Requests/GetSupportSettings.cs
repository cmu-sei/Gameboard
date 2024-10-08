using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

public record GetSupportSettingsQuery() : IRequest<SupportSettingsViewModel>;

internal class GetSupportSettingsHandler(IStore store, IValidatorService validatorService) : IRequestHandler<GetSupportSettingsQuery, SupportSettingsViewModel>
{
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<SupportSettingsViewModel> Handle(GetSupportSettingsQuery request, CancellationToken cancellationToken)
    {
        // validate
        await _validatorService
            .Auth(a => a.RequireAuthentication())
            .Validate(cancellationToken);

        // provide a default if no one has created settings yet
        var existingSettings = await _store
            .WithNoTracking<SupportSettings>()
                .Include(s => s.AutoTags)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingSettings is null)
            return new SupportSettingsViewModel
            {
                AutoTags = [],
                SupportPageGreeting = null
            };

        return new SupportSettingsViewModel
        {
            AutoTags = existingSettings.AutoTags.ToArray(),
            SupportPageGreeting = existingSettings.SupportPageGreeting
        };
    }
}
