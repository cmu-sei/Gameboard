using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Sponsors;

public record UpdateSponsorCommand(UpdateSponsorRequest Model, User ActingUser) : IRequest<Sponsor>;

internal class UpdateSponsorHandler(
    IMapper mapper,
    EntityExistsValidator<UpdateSponsorCommand, Data.Sponsor> sponsorExists,
    IStore store,
    IValidatorService<UpdateSponsorCommand> validatorService
    ) : IRequestHandler<UpdateSponsorCommand, Sponsor>
{
    private readonly IMapper _mapper = mapper;
    private readonly EntityExistsValidator<UpdateSponsorCommand, Data.Sponsor> _sponsorExists = sponsorExists;
    private readonly IStore _store = store;
    private readonly IValidatorService<UpdateSponsorCommand> _validatorService = validatorService;

    public async Task<Sponsor> Handle(UpdateSponsorCommand request, CancellationToken cancellationToken)
    {
        // validate/authorize
        await _validatorService
            .ConfigureAuthorization(a => a.RequirePermissions(UserRolePermissionKey.Sponsors_CreateEdit))
            .AddValidator(_sponsorExists.UseProperty(r => r.Model.Id))
            .AddValidator((req, ctx) =>
            {
                if (req.Model.Id == req.Model.ParentSponsorId)
                    ctx.AddValidationException(new CantSetSponsorAsParentOfItself(req.Model.Id));
            })
            .Validate(request, cancellationToken);

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
