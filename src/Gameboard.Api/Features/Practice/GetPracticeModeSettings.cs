using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeSettingsQuery(User ActingUser) : IRequest<PracticeModeSettingsApiModel>;

internal class GetPracticeModeSettingsHandler : IRequestHandler<GetPracticeModeSettingsQuery, PracticeModeSettingsApiModel>
{
    private readonly IPracticeService _practiceService;

    public GetPracticeModeSettingsHandler(IPracticeService practiceService)
        => _practiceService = practiceService;

    public Task<PracticeModeSettingsApiModel> Handle(GetPracticeModeSettingsQuery request, CancellationToken cancellationToken)
    {
        return _practiceService.GetSettings(cancellationToken);
    }
}
