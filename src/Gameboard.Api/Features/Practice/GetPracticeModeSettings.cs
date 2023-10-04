using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record GetPracticeModeSettingsQuery(User ActingUser) : IRequest<PracticeModeSettingsApiModel>;

internal class GetPracticeModeSettingsHandler : IRequestHandler<GetPracticeModeSettingsQuery, PracticeModeSettingsApiModel>
{
    private readonly IMapper _mapper;
    private readonly IPracticeService _practiceService;
    private readonly IStore _store;

    public GetPracticeModeSettingsHandler(IMapper mapper, IPracticeService practiceService, IStore store)
    {
        _mapper = mapper;
        _practiceService = practiceService;
        _store = store;
    }

    public async Task<PracticeModeSettingsApiModel> Handle(GetPracticeModeSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _store.FirstOrDefaultAsync<PracticeModeSettings>(cancellationToken);

        // if we don't have any settings, make up some defaults
        if (settings is null)
        {
            return new PracticeModeSettingsApiModel
            {
                CertificateHtmlTemplate = null,
                DefaultPracticeSessionLengthMinutes = 60,
                IntroTextMarkdown = null,
                SuggestedSearches = Array.Empty<string>()
            };
        }

        var apiModel = _mapper.Map<PracticeModeSettingsApiModel>(settings);
        apiModel.SuggestedSearches = _practiceService.UnescapeSuggestedSearches(settings.SuggestedSearches);

        return apiModel;
    }
}
