using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Sponsors;

public record UpdateSponsorCommand(ChangedSponsor Model, User ActingUser) : IRequest;

internal class UpdateSponsorHandler : IRequestHandler<UpdateSponsorCommand>
{
    private readonly EntityExistsValidator<UpdateSponsorCommand, Data.Sponsor> _sponsorExists;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<UpdateSponsorCommand> _validatorService;

    public UpdateSponsorHandler
    (
        EntityExistsValidator<UpdateSponsorCommand, Data.Sponsor> sponsorExists,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<UpdateSponsorCommand> validatorService
    )
    {
        _sponsorExists = sponsorExists;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task Handle(UpdateSponsorCommand request, CancellationToken cancellationToken)
    {
        // validate/authorize
        _userRoleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Registrar };
        _userRoleAuthorizer.Authorize();

        _validatorService.AddValidator(_sponsorExists.UseProperty(r => r.Model.Id));
        await _validatorService.Validate(request);

        // update
        var sponsor = await _store
            .WithNoTracking<Data.Sponsor>()
            .Where(s => s.Id == request.Model.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(s => s.Name, request.Model.Name), cancellationToken: cancellationToken);
    }
}
