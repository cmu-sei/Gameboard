using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Sponsors;

public record DeleteSponsorCommand(string SponsorId, User ActingUser) : IRequest;

internal class DeleteSponsorHandler : IRequestHandler<DeleteSponsorCommand>
{
    private readonly CoreOptions _options;
    private readonly EntityExistsValidator<DeleteSponsorCommand, Data.Sponsor> _sponsorExists;
    private readonly SponsorService _sponsorService;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<DeleteSponsorCommand> _validatorService;

    public DeleteSponsorHandler
    (
        CoreOptions options,
        EntityExistsValidator<DeleteSponsorCommand, Data.Sponsor> sponsorExists,
        SponsorService sponsorService,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<DeleteSponsorCommand> validatorService
    )
    {
        _options = options;
        _sponsorExists = sponsorExists;
        _sponsorService = sponsorService;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task Handle(DeleteSponsorCommand request, CancellationToken cancellationToken)
    {
        // authorize/validate
        _userRoleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Registrar, UserRole.Admin };
        _userRoleAuthorizer.Authorize();

        _validatorService.AddValidator(_sponsorExists.UseProperty(r => r.SponsorId));
        await _validatorService.Validate(request);

        // get the sponsor (including its sponsored users and players)
        // because we need to update their sponsor to the default
        var entity = await _store
            .WithNoTracking<Data.Sponsor>()
            .Include(s => s.SponsoredPlayers)
            .Include(s => s.SponsoredUsers)
            .SingleAsync(s => s.Id == request.SponsorId);

        // if this sponsor sponsors any players or users, we need to update them
        if (entity.SponsoredPlayers.Any() || entity.SponsoredUsers.Any())
        {
            var defaultSponsor = await _sponsorService.GetDefaultSponsor();

            if (entity.SponsoredPlayers.Any())
            {
                await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.SponsorId == request.SponsorId)
                    .ExecuteUpdateAsync(p => p.SetProperty(p => p.SponsorId, defaultSponsor.Id), cancellationToken);
            }

            if (entity.SponsoredUsers.Any())
            {
                await _store
                    .WithNoTracking<Data.User>()
                    .Where(u => u.SponsorId == request.SponsorId)
                    .ExecuteUpdateAsync
                    (
                        u => u
                            .SetProperty(u => u.SponsorId, defaultSponsor.Id)
                            .SetProperty(u => u.HasDefaultSponsor, true),
                        cancellationToken
                    );
            }
        }

        // now delete the entity
        await _store.Delete<Data.Sponsor>(request.SponsorId);
        if (entity.Logo.IsEmpty())
            return;

        // check for their logo file and delete that too
        if (entity.Logo.NotEmpty())
            _sponsorService.DeleteLogoFileByName(entity.Logo);
    }
}
