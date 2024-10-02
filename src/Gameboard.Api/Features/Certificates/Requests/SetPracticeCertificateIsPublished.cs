using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Certificates;

public record SetPracticeCertificateIsPublishedCommand(string ChallengeSpecId, bool IsPublished, User ActingUser) : IRequest<PublishedCertificateViewModel>;

internal class SetPracticeCertificateIsPublishedHandler : IRequestHandler<SetPracticeCertificateIsPublishedCommand, PublishedCertificateViewModel>
{
    private readonly IGuidService _guidService;
    private readonly INowService _nowService;
    private readonly EntityExistsValidator<SetPracticeCertificateIsPublishedCommand, Data.ChallengeSpec> _specExists;
    private readonly IStore _store;
    private readonly IValidatorService<SetPracticeCertificateIsPublishedCommand> _validator;

    public SetPracticeCertificateIsPublishedHandler
    (
        IGuidService guidService,
        INowService nowService,
        EntityExistsValidator<SetPracticeCertificateIsPublishedCommand, Data.ChallengeSpec> specExists,
        IStore store,
        IValidatorService<SetPracticeCertificateIsPublishedCommand> validator
    )
    {
        _guidService = guidService;
        _nowService = nowService;
        _specExists = specExists;
        _store = store;
        _validator = validator;
    }

    public async Task<PublishedCertificateViewModel> Handle(SetPracticeCertificateIsPublishedCommand request, CancellationToken cancellationToken)
    {
        _validator.AddValidator(_specExists.UseProperty(r => r.ChallengeSpecId));
        await _validator.Validate(request, cancellationToken);

        // pull the existing publish if it exists - we need it for return on the unpublish case
        var existingPublish = await GetExistingPublish(request.ActingUser.Id, request.ChallengeSpecId, cancellationToken);

        if (request.IsPublished && existingPublish is null)
        {
            var publish = new PublishedPracticeCertificate
            {
                Id = _guidService.Generate(),
                OwnerUserId = request.ActingUser.Id,
                PublishedOn = _nowService.Get(),
                Mode = PublishedCertificateMode.Practice,
                ChallengeSpecId = request.ChallengeSpecId
            };
            await _store.Create(publish);

            // pull the challenge spec and owner for return val
            var createdPublish = await GetExistingPublish(request.ActingUser.Id, request.ChallengeSpecId, cancellationToken);

            return new PublishedCertificateViewModel
            {
                Id = createdPublish.Id,
                PublishedOn = createdPublish.PublishedOn,
                AwardedForEntity = new SimpleEntity { Id = createdPublish.ChallengeSpecId, Name = createdPublish.ChallengeSpec.Name },
                OwnerUser = new SimpleEntity { Id = createdPublish.OwnerUser.Id, Name = createdPublish.OwnerUser.ApprovedName },
                Mode = PublishedCertificateMode.Practice
            };
        }
        else if (!request.IsPublished && existingPublish is not null)
        {
            await _store
                .WithNoTracking<PublishedPracticeCertificate>()
                .Where(c => c.ChallengeSpecId == request.ChallengeSpecId)
                .Where(c => c.OwnerUserId == request.ActingUser.Id)
                .ExecuteDeleteAsync(cancellationToken);

            return new PublishedCertificateViewModel
            {
                Id = existingPublish.Id,
                PublishedOn = null,
                AwardedForEntity = new SimpleEntity { Id = existingPublish.ChallengeSpecId, Name = existingPublish.ChallengeSpec.Name },
                OwnerUser = new SimpleEntity { Id = existingPublish.OwnerUser.Id, Name = existingPublish.OwnerUser.ApprovedName },
                Mode = PublishedCertificateMode.Practice
            };
        }

        return null;
    }

    private async Task<PublishedPracticeCertificate> GetExistingPublish(string ownerUserId, string challengeSpecId, CancellationToken cancellationToken)
        => await _store
            .WithNoTracking<PublishedPracticeCertificate>()
                .Include(c => c.ChallengeSpec)
                .Include(c => c.OwnerUser)
            .Where(c => c.ChallengeSpecId == challengeSpecId)
            .Where(c => c.OwnerUserId == ownerUserId)
            .FirstOrDefaultAsync(cancellationToken);
}
