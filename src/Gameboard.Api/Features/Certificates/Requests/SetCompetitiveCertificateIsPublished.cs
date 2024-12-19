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

public record SetCompetitiveCertificateIsPublishedCommand(string GameId, bool IsPublished, User ActingUser) : IRequest<PublishedCertificateViewModel>;

internal class SetCompetitiveCertificateIsPublishedHandler : IRequestHandler<SetCompetitiveCertificateIsPublishedCommand, PublishedCertificateViewModel>
{
    private readonly EntityExistsValidator<SetCompetitiveCertificateIsPublishedCommand, Data.Game> _gameExists;
    private readonly IGuidService _guidService;
    private readonly INowService _nowService;
    private readonly IStore _store;
    private readonly IValidatorService<SetCompetitiveCertificateIsPublishedCommand> _validator;

    public SetCompetitiveCertificateIsPublishedHandler
    (
        EntityExistsValidator<SetCompetitiveCertificateIsPublishedCommand, Data.Game> gameExists,
        IGuidService guidService,
        INowService nowService,
        IStore store,
        IValidatorService<SetCompetitiveCertificateIsPublishedCommand> validator
    )
    {
        _guidService = guidService;
        _nowService = nowService;
        _gameExists = gameExists;
        _store = store;
        _validator = validator;
    }

    public async Task<PublishedCertificateViewModel> Handle(SetCompetitiveCertificateIsPublishedCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .Validate(request, cancellationToken);

        // pull the existing publish if it exists - we need it for return on the unpublish case
        var existingPublish = await GetExistingPublish(request.ActingUser.Id, request.GameId, cancellationToken);

        if (request.IsPublished && existingPublish is null)
        {
            var publish = new PublishedCompetitiveCertificate
            {
                Id = _guidService.Generate(),
                OwnerUserId = request.ActingUser.Id,
                PublishedOn = _nowService.Get(),
                GameId = request.GameId
            };
            await _store.Create(publish);

            // pull the game and owner for return val
            var createdPublish = await GetExistingPublish(request.ActingUser.Id, request.GameId, cancellationToken);

            return new PublishedCertificateViewModel
            {
                Id = createdPublish.Id,
                PublishedOn = createdPublish.PublishedOn,
                AwardedForEntity = new SimpleEntity { Id = createdPublish.GameId, Name = createdPublish.Game.Name },
                OwnerUser = new SimpleEntity { Id = createdPublish.OwnerUser.Id, Name = createdPublish.OwnerUser.ApprovedName },
                Mode = PublishedCertificateMode.Competitive
            };
        }
        else if (!request.IsPublished && existingPublish is not null)
        {
            await _store
                .WithNoTracking<PublishedCompetitiveCertificate>()
                .Where(c => c.GameId == request.GameId)
                .Where(c => c.OwnerUserId == request.ActingUser.Id)
                .ExecuteDeleteAsync(cancellationToken);

            return new PublishedCertificateViewModel
            {
                Id = existingPublish.Id,
                PublishedOn = null,
                AwardedForEntity = new SimpleEntity { Id = existingPublish.GameId, Name = existingPublish.Game.Name },
                OwnerUser = new SimpleEntity { Id = existingPublish.OwnerUser.Id, Name = existingPublish.OwnerUser.ApprovedName },
                Mode = PublishedCertificateMode.Competitive
            };
        }

        return null;
    }

    private Task<PublishedCompetitiveCertificate> GetExistingPublish(string ownerUserId, string gameId, CancellationToken cancellationToken)
        => _store
            .WithNoTracking<PublishedCompetitiveCertificate>()
                .Include(c => c.Game)
                .Include(c => c.OwnerUser)
            .Where(c => c.GameId == gameId)
            .Where(c => c.OwnerUserId == ownerUserId)
            .OrderByDescending(c => c.PublishedOn)
            .FirstOrDefaultAsync(cancellationToken);
}
