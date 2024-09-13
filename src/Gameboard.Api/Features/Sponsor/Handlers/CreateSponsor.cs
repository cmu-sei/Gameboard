using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Sponsors;

public record CreateSponsorCommand(NewSponsor Model, User ActingUser) : IRequest<SponsorWithParentSponsor>;

internal class CreateSponsorHandler(
    IMapper mapper,
    IStore store,
    IValidatorService<CreateSponsorCommand> validatorService
    ) : IRequestHandler<CreateSponsorCommand, SponsorWithParentSponsor>
{
    private readonly IMapper _mapper = mapper;
    private readonly IStore _store = store;
    private readonly IValidatorService<CreateSponsorCommand> _validatorService = validatorService;

    public async Task<SponsorWithParentSponsor> Handle(CreateSponsorCommand request, CancellationToken cancellationToken)
    {
        // authorize/validate
        await _validatorService
            .Auth(a => a.RequirePermissions(PermissionKey.Sponsors_CreateEdit))
            .AddValidator((request, context) =>
            {
                if (request.Model.Name.IsEmpty())
                    context.AddValidationException(new MissingRequiredInput<string>(nameof(request.Model.Name), request.Model.Name));
            })
            .Validate(request, cancellationToken);

        // create sponsor without logo - we can upload after
        var sponsor = await _store.Create(new Data.Sponsor
        {
            Approved = true,
            Name = request.Model.Name,
            ParentSponsorId = request.Model.ParentSponsorId.IsNotEmpty() ? request.Model.ParentSponsorId : null
        });

        var response = new SponsorWithParentSponsor
        {
            Id = sponsor.Id,
            Name = sponsor.Name,
            Logo = string.Empty,
            ParentSponsor = null
        };

        if (request.Model.ParentSponsorId.IsNotEmpty())
        {
            // pull the parent and return it with the response
            response.ParentSponsor = _mapper.Map<Sponsor>(await _store.WithNoTracking<Data.Sponsor>().SingleAsync(s => s.Id == request.Model.ParentSponsorId, cancellationToken));
        }

        return response;
    }
}
