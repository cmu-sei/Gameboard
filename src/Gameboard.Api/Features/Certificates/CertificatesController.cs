using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Certificates;

[Authorize]
[Route("/api/user")]
public class CertificatesController : ControllerBase
{
    private readonly IActingUserService _actingUser;
    private readonly IHtmlToImageService _htmlToImage;
    private readonly IMediator _mediator;

    public CertificatesController
    (
        IActingUserService actingUser,
        IHtmlToImageService htmlToImage,
        IMediator mediator
    )
    {
        _actingUser = actingUser;
        _htmlToImage = htmlToImage;
        _mediator = mediator;
    }

    [HttpGet]
    [Route("{userId}/certificates")]
    public Task<IEnumerable<PracticeModeCertificate>> ListCertificates([FromRoute] string userId)
        => _mediator.Send(new GetPracticeModeCertificatesQuery(userId, _actingUser.Get()));

    // [HttpGet]
    // [Route("certificate/{challengeSpecId}/pdf")]
    // public async Task<FileResult> GetCertificatePdf([FromRoute] string challengeSpecId, CancellationToken cancellationToken)
    // {
    //     var html = await _mediator.Send(new GetPracticeModeCertificateHtmlQuery(challengeSpecId, _actingUserService.Get()), cancellationToken);
    //     return File(await _htmlToPdfService.ToPdf($"{_actingUserService.Get().Id}_{challengeSpecId}", html, 3300, 2550), MimeTypes.ApplicationPdf);
    // }

    [HttpGet]
    [Route("{userId}/certificates/{challengeSpecId}")]
    public async Task<FileResult> GetCertificatePng([FromRoute] string userId, [FromRoute] string challengeSpecId, CancellationToken cancellationToken)
    {
        var html = await _mediator.Send(new GetPracticeModeCertificatePngQuery(challengeSpecId, userId, _actingUser.Get()), cancellationToken);
        return File(await _htmlToImage.ToPng($"{_actingUser.Get().Id}_{challengeSpecId}", html, 3300, 2550), MimeTypes.ImagePng);
    }
}
