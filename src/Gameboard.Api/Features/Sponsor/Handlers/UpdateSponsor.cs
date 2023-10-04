using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Sponsors;

public record UpdateSponsorCommand(UpdateSponsorRequest Model, User ActingUser) : IRequest<Sponsor>;

internal class UpdateSponsorHandler : IRequestHandler<UpdateSponsorCommand, Sponsor>
{
    private readonly IMapper _mapper;
    private readonly EntityExistsValidator<UpdateSponsorCommand, Data.Sponsor> _sponsorExists;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<UpdateSponsorCommand> _validatorService;

    public UpdateSponsorHandler
    (
        IMapper mapper,
        EntityExistsValidator<UpdateSponsorCommand, Data.Sponsor> sponsorExists,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<UpdateSponsorCommand> validatorService
    )
    {
        _mapper = mapper;
        _sponsorExists = sponsorExists;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<Sponsor> Handle(UpdateSponsorCommand request, CancellationToken cancellationToken)
    {
        // validate/authorize
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Registrar)
            .Authorize();

        _validatorService.AddValidator(_sponsorExists.UseProperty(r => r.Model.Id));
        await _validatorService.Validate(request, cancellationToken);

        // update
        var sponsor = await _store
            .WithNoTracking<Data.Sponsor>()
            .Where(s => s.Id == request.Model.Id)
            .ExecuteUpdateAsync
            (
                s => s
                    .SetProperty(s => s.Name, request.Model.Name)
                    .SetProperty(s => s.ParentSponsorId, request.Model.ParentSponsorId),
                    cancellationToken: cancellationToken
            );

        return _mapper.Map<Sponsor>(await _store.SingleAsync<Data.Sponsor>(request.Model.Id, cancellationToken));
    }
}
