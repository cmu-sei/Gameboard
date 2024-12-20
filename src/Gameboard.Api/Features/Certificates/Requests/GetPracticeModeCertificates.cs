using Gameboard.Api.Features.Certificates;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeCertificatesQuery(string CertificateOwnerUserId, User ActingUser) : IRequest<IEnumerable<PracticeModeCertificate>>;

internal class GetPracticeModeCertificatesHandler
(
    ICertificatesService certificatesService,
    EntityExistsValidator<GetPracticeModeCertificatesQuery, Data.User> userExists,
    IValidatorService<GetPracticeModeCertificatesQuery> validatorService
) : IRequestHandler<GetPracticeModeCertificatesQuery, IEnumerable<PracticeModeCertificate>>
{
    private readonly ICertificatesService _certificatesService = certificatesService;
    private readonly EntityExistsValidator<GetPracticeModeCertificatesQuery, Data.User> _userExists = userExists;
    private readonly IValidatorService<GetPracticeModeCertificatesQuery> _validatorService = validatorService;

    public async Task<IEnumerable<PracticeModeCertificate>> Handle(GetPracticeModeCertificatesQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth
            (
                a => a
                    .RequirePermissions(Users.PermissionKey.Admin_View)
                    .UnlessUserIdIn(request.CertificateOwnerUserId)
            )
            .AddValidator(_userExists.UseProperty(r => r.CertificateOwnerUserId))
            .Validate(request, cancellationToken);

        return await _certificatesService.GetPracticeCertificates(request.CertificateOwnerUserId, cancellationToken);
    }
}
