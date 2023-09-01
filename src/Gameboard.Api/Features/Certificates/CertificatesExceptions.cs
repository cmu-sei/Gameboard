using Gameboard.Api.Data;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Certificates;

public class CertificateIsntPublished : GameboardValidationException
{
    public CertificateIsntPublished(string ownerUserId, PublishedCertificateMode mode, string entityId)
        : base($"""There is no published certificate for user "{ownerUserId}" playing entity "{entityId}" in {mode.ToString()} mode.""") { }
}
