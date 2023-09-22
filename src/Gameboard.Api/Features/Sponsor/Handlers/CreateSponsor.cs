using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Sponsors;

public record CreateSponsorCommand(NewSponsor Model, User ActingUser) : IRequest<SponsorWithParentSponsor>;

internal class CreateSponsorHandler : IRequestHandler<CreateSponsorCommand, SponsorWithParentSponsor>
{
    private readonly ContentTypeValidator<CreateSponsorCommand> _legalLogoType;
    private readonly IMapper _mapper;
    private readonly SponsorService _sponsorService;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<CreateSponsorCommand> _validatorService;

    public CreateSponsorHandler
    (
        ContentTypeValidator<CreateSponsorCommand> legalLogoType,
        IMapper mapper,
        SponsorService sponsorService,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<CreateSponsorCommand> validatorService
    )
    {
        _legalLogoType = legalLogoType;
        _mapper = mapper;
        _sponsorService = sponsorService;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<SponsorWithParentSponsor> Handle(CreateSponsorCommand request, CancellationToken cancellationToken)
    {
        // authorize/validate
        _userRoleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Registrar };
        _userRoleAuthorizer.Authorize();

        _validatorService.AddValidator
        (
            _legalLogoType
                .HasPermittedTypes(_sponsorService.GetAllowedLogoMimeTypes())
                .UseProperty(r => r.Model.LogoFile)
        );

        _validatorService.AddValidator((request, context) =>
        {
            if (request.Model.Name.IsEmpty())
                context.AddValidationException(new MissingRequiredInput<string>(nameof(request.Model.Name), request.Model.Name));

            return Task.CompletedTask;
        });
        await _validatorService.Validate(request);

        // create sponsor without logo - we can upload after
        var sponsor = await _store.Create(new Data.Sponsor
        {
            Approved = true,
            Name = request.Model.Name,
            ParentSponsorId = request.Model.ParentSponsorId.IsNotEmpty() ? request.Model.ParentSponsorId : null
        });

        // if they have a logo file, add that and clean up the old one
        var logoFileName = string.Empty;
        if (request.Model.LogoFile is not null)
            logoFileName = await _sponsorService.SetLogo(sponsor.Id, request.Model.LogoFile, cancellationToken);

        var response = new SponsorWithParentSponsor
        {
            Id = sponsor.Id,
            Name = sponsor.Name,
            Logo = logoFileName,
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
