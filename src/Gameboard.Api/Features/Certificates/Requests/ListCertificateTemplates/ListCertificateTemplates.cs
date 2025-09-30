// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Certificates;

public record ListCertificateTemplatesQuery() : IRequest<IEnumerable<CertificateTemplateView>>;

internal sealed class ListCertificateTemplatesHandler
(
    ICertificatesService certificatesService,
    IValidatorService validator
) : IRequestHandler<ListCertificateTemplatesQuery, IEnumerable<CertificateTemplateView>>
{
    private readonly ICertificatesService _certificatesService = certificatesService;
    private readonly IValidatorService _validator = validator;

    public async Task<IEnumerable<CertificateTemplateView>> Handle(ListCertificateTemplatesQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.Require(PermissionKey.Games_CreateEditDelete))
            .Validate(cancellationToken);

        return await _certificatesService
            .GetTemplatesQuery()
            .ToArrayAsync(cancellationToken);
    }
}
