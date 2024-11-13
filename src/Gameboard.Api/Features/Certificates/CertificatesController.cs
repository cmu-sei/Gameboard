using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Certificates;

[Authorize]
[ApiController]
[Route("/api/user")]
public class CertificatesController(
    IActingUserService actingUser,
    IHtmlToImageService htmlToImage,
    IMediator mediator
    ) : ControllerBase
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly IHtmlToImageService _htmlToImage = htmlToImage;
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [Route("{userId}/certificates/practice")]
    public Task<IEnumerable<PracticeModeCertificate>> ListCertificates([FromRoute] string userId)
        => _mediator.Send(new GetPracticeModeCertificatesQuery(userId, _actingUser.Get()));

    [HttpPost]
    [Route("{userId}/certificates/competitive/{gameId}")]
    public Task<PublishedCertificateViewModel> PublishCompetitiveCertificate([FromRoute] string gameId, CancellationToken cancellationToken)
        => _mediator.Send(new SetCompetitiveCertificateIsPublishedCommand(gameId, true, _actingUser.Get()), cancellationToken);

    [HttpDelete]
    [Route("{userId}/certificates/competitive/{gameId}")]
    public Task<PublishedCertificateViewModel> UnpublishCompetitiveCertificate([FromRoute] string gameId, CancellationToken cancellationToken)
        => _mediator.Send(new SetCompetitiveCertificateIsPublishedCommand(gameId, false, _actingUser.Get()), cancellationToken);

    [HttpPost]
    [Route("{userId}/certificates/practice/{challengeSpecId}")]
    public Task<PublishedCertificateViewModel> PublishPracticeCertificate([FromRoute] string challengeSpecId, CancellationToken cancellationToken)
        => _mediator.Send(new SetPracticeCertificateIsPublishedCommand(challengeSpecId, true, _actingUser.Get()), cancellationToken);

    [HttpDelete]
    [Route("{userId}/certificates/practice/{challengeSpecId}")]
    public Task<PublishedCertificateViewModel> UnpublishPracticeCertificate([FromRoute] string challengeSpecId, CancellationToken cancellationToken)
        => _mediator.Send(new SetPracticeCertificateIsPublishedCommand(challengeSpecId, false, _actingUser.Get()), cancellationToken);

    [HttpGet]
    [Route("{userId}/certificates/practice/{awardedForEntityId}")]
    [AllowAnonymous] // anyone can _try_, but we only serve them the cert if it's published (or if they're the owner)
    public async Task<FileResult> GetPracticeCertificatePng([FromRoute] string userId, [FromRoute] string awardedForEntityId, [FromQuery] string requestedNameOverride, CancellationToken cancellationToken)
    {
        var html = await _mediator.Send(new GetPracticeModeCertificateHtmlQuery(awardedForEntityId, userId, _actingUser.Get(), requestedNameOverride), cancellationToken);
        return File(await _htmlToImage.ToPng($"{_actingUser.Get().Id}_{awardedForEntityId}", html, 3300, 2550), MimeTypes.ImagePng);
    }

    [HttpGet]
    [Route("{userId}/certificates/practice/{awardedForEntityId}/pdf")]
    [AllowAnonymous] // anyone can _try_, but we only serve them the cert if it's published (or if they're the owner)
    public async Task<FileResult> GetPracticeCertificatePdf([FromRoute] string userId, [FromRoute] string awardedForEntityId, [FromQuery] string requestedNameOverride, CancellationToken cancellationToken)
    {
        var html = await _mediator.Send(new GetPracticeModeCertificateHtmlQuery(awardedForEntityId, userId, _actingUser.Get(), requestedNameOverride), cancellationToken);
        return File(await _htmlToImage.ToPdf($"{_actingUser.Get().Id}_{awardedForEntityId}", html, 3300, 2550), MimeTypes.ApplicationPdf);
    }

    [HttpGet]
    [Route("{userId}/certificates/competitive/{awardedForEntityId}")]
    [AllowAnonymous] // anyone can _try_, but we only serve them the cert if it's published (or if they're the owner)
    public async Task<FileResult> GetCompetitiveCertificatePng([FromRoute] string userId, [FromRoute] string awardedForEntityId, CancellationToken cancellationToken)
    {
        var html = await _mediator.Send(new GetCompetitiveModeCertificateHtmlQuery(awardedForEntityId, userId, _actingUser.Get().Id), cancellationToken);
        return File(await _htmlToImage.ToPng($"{_actingUser.Get().Id}_{awardedForEntityId}", html, 3300, 2550), MimeTypes.ImagePng);
    }
}
