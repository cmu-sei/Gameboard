using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeCertificatesQuery(User ActingUser) : IRequest<IEnumerable<PracticeModeCertificate>>;

internal class GetPracticeModeCertificatesHandler : IRequestHandler<GetPracticeModeCertificatesQuery, IEnumerable<PracticeModeCertificate>>
{
    private readonly IPracticeService _practiceService;
    private readonly EntityExistsValidator<GetPracticeModeCertificatesQuery, Data.User> _userExists;
    private readonly IValidatorService<GetPracticeModeCertificatesQuery> _validatorService;

    public GetPracticeModeCertificatesHandler
    (
        IPracticeService practiceService,
        EntityExistsValidator<GetPracticeModeCertificatesQuery, Data.User> userExists,
        IValidatorService<GetPracticeModeCertificatesQuery> validatorService
    )
    {
        _practiceService = practiceService;
        _userExists = userExists;
        _validatorService = validatorService;
    }

    public async Task<IEnumerable<PracticeModeCertificate>> Handle(GetPracticeModeCertificatesQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_userExists.UseProperty(r => r.ActingUser.Id));
        await _validatorService.Validate(request);

        return await _practiceService.GetCertificates(request.ActingUser.Id);
    }
}
