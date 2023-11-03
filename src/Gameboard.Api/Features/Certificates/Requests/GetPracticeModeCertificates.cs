using Gameboard.Api.Features.Certificates;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeCertificatesQuery(string CertificateOwnerUserId, User ActingUser) : IRequest<IEnumerable<PracticeModeCertificate>>;

internal class GetPracticeModeCertificatesHandler : IRequestHandler<GetPracticeModeCertificatesQuery, IEnumerable<PracticeModeCertificate>>
{
    private readonly ICertificatesService _certificatesService;
    private readonly EntityExistsValidator<GetPracticeModeCertificatesQuery, Data.User> _userExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetPracticeModeCertificatesQuery> _validatorService;

    public GetPracticeModeCertificatesHandler
    (
        ICertificatesService certificatesService,
        EntityExistsValidator<GetPracticeModeCertificatesQuery, Data.User> userExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetPracticeModeCertificatesQuery> validatorService
    )
    {
        _certificatesService = certificatesService;
        _userExists = userExists;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<IEnumerable<PracticeModeCertificate>> Handle(GetPracticeModeCertificatesQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin)
            .AllowUserId(request.CertificateOwnerUserId)
            .Authorize();

        _validatorService.AddValidator(_userExists.UseProperty(r => r.CertificateOwnerUserId));
        await _validatorService.Validate(request, cancellationToken);

        return await _certificatesService.GetPracticeCertificates(request.CertificateOwnerUserId);
    }
}
