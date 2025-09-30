// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Certificates;

public record GetCertificateTemplatePreviewHtml(string TemplateId) : IRequest<string>;

internal sealed class GetCertificateTemplatePreviewHtmlHandler
(
    ICertificatesService certificatesService,
    IValidatorService validator
) : IRequestHandler<GetCertificateTemplatePreviewHtml, string>
{
    private readonly ICertificatesService _certificatesService = certificatesService;
    private readonly IValidatorService _validator = validator;

    public async Task<string> Handle(GetCertificateTemplatePreviewHtml request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.Require(PermissionKey.Admin_View))
            .AddEntityExistsValidator<CertificateTemplate>(request.TemplateId)
            .Validate(cancellationToken);

        return await _certificatesService.BuildCertificateTemplatePreviewHtml(request.TemplateId, cancellationToken);
    }
}
