// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
[Route("/api")]
public class CertificatesController
(
    IActingUserService actingUser,
    IHtmlToImageService htmlToImage,
    IMediator mediator
) : ControllerBase
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly IHtmlToImageService _htmlToImage = htmlToImage;
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [Route("user/{userId}/certificates/competitive")]
    public Task<IEnumerable<CompetitiveModeCertificate>> ListCompetitiveCertificates([FromRoute] string userId)
        => _mediator.Send(new GetCompetitiveCertificatesQuery(userId));

    [HttpGet]
    [Route("user/{userId}/certificates/practice")]
    public Task<IEnumerable<PracticeModeCertificate>> ListCertificates([FromRoute] string userId)
        => _mediator.Send(new GetPracticeModeCertificatesQuery(userId, _actingUser.Get()));

    [HttpPost]
    [Route("user/{userId}/certificates/competitive/{gameId}")]
    public Task<PublishedCertificateViewModel> PublishCompetitiveCertificate([FromRoute] string gameId, CancellationToken cancellationToken)
        => _mediator.Send(new SetCompetitiveCertificateIsPublishedCommand(gameId, true, _actingUser.Get()), cancellationToken);

    [HttpDelete]
    [Route("user/{userId}/certificates/competitive/{gameId}")]
    public Task<PublishedCertificateViewModel> UnpublishCompetitiveCertificate([FromRoute] string gameId, CancellationToken cancellationToken)
        => _mediator.Send(new SetCompetitiveCertificateIsPublishedCommand(gameId, false, _actingUser.Get()), cancellationToken);

    [HttpPost]
    [Route("user/{userId}/certificates/practice/{challengeSpecId}")]
    public Task<PublishedCertificateViewModel> PublishPracticeCertificate([FromRoute] string challengeSpecId, CancellationToken cancellationToken)
        => _mediator.Send(new SetPracticeCertificateIsPublishedCommand(challengeSpecId, true, _actingUser.Get()), cancellationToken);

    [HttpDelete]
    [Route("user/{userId}/certificates/practice/{challengeSpecId}")]
    public Task<PublishedCertificateViewModel> UnpublishPracticeCertificate([FromRoute] string challengeSpecId, CancellationToken cancellationToken)
        => _mediator.Send(new SetPracticeCertificateIsPublishedCommand(challengeSpecId, false, _actingUser.Get()), cancellationToken);

    [HttpGet]
    [Route("user/{userId}/certificates/practice/{awardedForEntityId}")]
    [AllowAnonymous] // anyone can _try_, but we only serve them the cert if it's published (or if they're the owner)
    public async Task<FileResult> GetPracticeCertificatePng([FromRoute] string userId, [FromRoute] string awardedForEntityId, [FromQuery] string requestedNameOverride, CancellationToken cancellationToken)
    {
        var html = await _mediator.Send(new GetPracticeModeCertificateHtmlQuery(awardedForEntityId, userId, _actingUser.Get(), requestedNameOverride), cancellationToken);
        return File(await _htmlToImage.ToPng($"{_actingUser.Get().Id}_{awardedForEntityId}.png", html, 3300, 2550), MimeTypes.ImagePng);
    }

    [HttpGet]
    [Route("user/{userId}/certificates/practice/{awardedForEntityId}/pdf")]
    [AllowAnonymous] // anyone can _try_, but we only serve them the cert if it's published (or if they're the owner)
    public async Task<FileResult> GetPracticeCertificatePdf([FromRoute] string userId, [FromRoute] string awardedForEntityId, [FromQuery] string requestedNameOverride, CancellationToken cancellationToken)
    {
        var html = await _mediator.Send(new GetPracticeModeCertificateHtmlQuery(awardedForEntityId, userId, _actingUser.Get(), requestedNameOverride), cancellationToken);
        return File(await _htmlToImage.ToPdf($"{_actingUser.Get().Id}_{awardedForEntityId}.pdf", html, 3300, 2550), MimeTypes.ApplicationPdf);
    }

    [HttpGet]
    [Route("user/{userId}/certificates/competitive/{awardedForEntityId}")]
    [AllowAnonymous] // anyone can _try_, but we only serve them the cert if it's published (or if they're the owner)
    public async Task<FileResult> GetCompetitiveCertificatePng([FromRoute] string userId, [FromRoute] string awardedForEntityId, [FromQuery] string requestedNameOverride, CancellationToken cancellationToken)
    {
        var html = await _mediator.Send(new GetCompetitiveModeCertificateHtmlQuery(awardedForEntityId, userId, requestedNameOverride), cancellationToken);
        return File(await _htmlToImage.ToPng($"{_actingUser.Get().Id}_{awardedForEntityId}.png", html, 3300, 2550), MimeTypes.ImagePng);
    }

    [HttpDelete]
    [Route("certificates/templates/{templateId}")]
    public Task DeleteTemplate([FromRoute] string templateId, CancellationToken cancellationToken)
        => _mediator.Send(new DeleteCertificateTemplateCommand(templateId), cancellationToken);

    [HttpGet]
    [Route("certificates/templates")]
    public Task<IEnumerable<CertificateTemplateView>> ListTemplates(CancellationToken cancellationToken)
        => _mediator.Send(new ListCertificateTemplatesQuery(), cancellationToken);

    [HttpGet]
    [Route("certificates/templates/{templateId}/preview")]
    public async Task<FileResult> GetTemplatePreview([FromRoute] string templateId, CancellationToken cancellationToken)
    {
        var html = await _mediator.Send(new GetCertificateTemplatePreviewHtml(templateId), cancellationToken);
        return File(await _htmlToImage.ToPng($"template_{templateId}_preview.png", html, 3300, 2550), MimeTypes.ImagePng);
    }

    [HttpPost]
    [Route("certificates/templates")]
    public Task<CertificateTemplateView> CreateTemplate([FromBody] UpsertCertificateTemplateRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new UpsertCertificateTemplateCommand(null, request), cancellationToken);

    [HttpPut]
    [Route("certificates/templates/{templateId}")]
    public Task<CertificateTemplateView> UpdateTemplate([FromRoute] string templateId, [FromBody] UpsertCertificateTemplateRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new UpsertCertificateTemplateCommand(templateId, request), cancellationToken);
}
