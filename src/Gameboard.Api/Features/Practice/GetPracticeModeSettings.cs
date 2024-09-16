using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeSettingsQuery(User ActingUser) : IRequest<PracticeModeSettingsApiModel>;

internal class GetPracticeModeSettingsHandler(IPracticeService practiceService) : IRequestHandler<GetPracticeModeSettingsQuery, PracticeModeSettingsApiModel>
{
    private readonly IPracticeService _practiceService = practiceService;

    public Task<PracticeModeSettingsApiModel> Handle(GetPracticeModeSettingsQuery request, CancellationToken cancellationToken)
        => _practiceService.GetSettings(cancellationToken);
}
