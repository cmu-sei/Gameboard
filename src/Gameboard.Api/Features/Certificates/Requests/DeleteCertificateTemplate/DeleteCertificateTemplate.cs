using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Certificates;

public record DeleteCertificateTemplateCommand(string TemplateId) : IRequest;

internal sealed class DeleteCertificateTemplateHandler
(
    IStore store,
    IValidatorService validator
) : IRequestHandler<DeleteCertificateTemplateCommand>
{
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validator;

    public async Task Handle(DeleteCertificateTemplateCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.RequirePermissions(PermissionKey.Games_CreateEditDelete))
            .AddEntityExistsValidator<CertificateTemplate>(request.TemplateId)
            .Validate(cancellationToken);

        await _store
            .WithNoTracking<CertificateTemplate>()
            .Where(t => t.Id == request.TemplateId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
