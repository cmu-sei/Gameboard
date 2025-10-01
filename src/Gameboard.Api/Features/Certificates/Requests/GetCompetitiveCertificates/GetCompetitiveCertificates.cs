// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Certificates;

public record GetCompetitiveCertificatesQuery(string OwnerUserId) : IRequest<IEnumerable<CompetitiveModeCertificate>>;

internal sealed class GetCompetitiveCertificatesHandler
(
    IActingUserService actingUser,
    ICertificatesService certificatesService,
    IValidatorService validator
) : IRequestHandler<GetCompetitiveCertificatesQuery, IEnumerable<CompetitiveModeCertificate>>
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly ICertificatesService _certificatesService = certificatesService;
    private readonly IValidatorService _validator = validator;

    public async Task<IEnumerable<CompetitiveModeCertificate>> Handle(GetCompetitiveCertificatesQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth
            (
                c => c
                    .RequireAuthentication()
                    .Require(PermissionKey.Admin_View)
                    .UnlessUserIdIn(request.OwnerUserId)
            )
            .Validate(cancellationToken);

        return await _certificatesService.GetCompetitiveCertificates(_actingUser.Get().Id, cancellationToken);
    }
}
