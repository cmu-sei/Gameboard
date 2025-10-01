// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Certificates;

public record UpsertCertificateTemplateCommand(string TemplateId, UpsertCertificateTemplateRequest Args) : IRequest<CertificateTemplateView>;

internal sealed class UpsertCertificateTemplateHandler
(
    IActingUserService actingUserService,
    ICertificatesService certificatesService,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<UpsertCertificateTemplateCommand, CertificateTemplateView>
{
    private readonly IActingUserService _actingUser = actingUserService;
    private readonly ICertificatesService _certificatesService = certificatesService;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validatorService;

    public async Task<CertificateTemplateView> Handle(UpsertCertificateTemplateCommand request, CancellationToken cancellationToken)
    {
        _validator
            .Auth(c => c.Require(Users.PermissionKey.Games_CreateEditDelete))
            .AddValidator(ctx =>
            {
                if (request.Args.Name.IsEmpty())
                {
                    ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.Args.Name)));
                }

                if (request.Args.Content.IsEmpty())
                {
                    ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.Args.Content)));
                }
            });

        if (request.TemplateId.IsNotEmpty())
        {
            _validator.AddEntityExistsValidator<CertificateTemplate>(request.TemplateId);
        }

        await _validator.Validate(cancellationToken);

        // let's party
        var upsertEntity = new CertificateTemplate
        {
            Id = request.TemplateId,
            Name = request.Args.Name,
            Content = request.Args.Content
        };

        if (request.TemplateId.IsEmpty())
        {
            upsertEntity.CreatedByUserId = _actingUser.Get().Id;
            await _store.Create(upsertEntity);
        }
        else
        {
            await _store
                .WithNoTracking<CertificateTemplate>()
                .Where(t => t.Id == request.TemplateId)
                .ExecuteUpdateAsync
                (
                    up => up
                        .SetProperty(c => c.Content, upsertEntity.Content)
                        .SetProperty(c => c.Name, upsertEntity.Name),
                    cancellationToken
                );
        }

        return await _certificatesService
            .GetTemplatesQuery()
            .SingleAsync(t => t.Id == upsertEntity.Id, cancellationToken);
    }
}
