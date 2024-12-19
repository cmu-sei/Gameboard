namespace Gameboard.Api.Features.Certificates;

public sealed class UpsertCertificateTemplateRequest
{
    public required string Name { get; set; }
    public required string Content { get; set; }
}
