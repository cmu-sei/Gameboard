using System.Linq;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IPlayersReportService
{
    IQueryable<Data.Player> GetPlayersReportBaseQuery(PlayersReportQueryParameters parameters);
}

internal class PlayersReportService : IPlayersReportService
{
    private readonly IPlayerStore _playerStore;
    private readonly IUserStore _userStore;

    public PlayersReportService(IPlayerStore playerStore, ISponsorStore sponsorStore, IUserStore userStore)
    {
        _playerStore = playerStore;
        _userStore = userStore;
    }

    public IQueryable<Data.Player> GetPlayersReportBaseQuery(PlayersReportQueryParameters parameters)
    {
        // compute these first so we can preserve server-side evaluation
        var hasGameId = parameters.GameId.NotEmpty();
        var hasSeries = parameters.Series.NotEmpty();
        var hasSessionStartBegin = parameters.SessionStartWindow != null && parameters.SessionStartWindow.HasDateStartValue();
        var hasSessionStartEnd = parameters.SessionStartWindow != null && parameters.SessionStartWindow.HasDateEndValue();
        var hasSpecId = parameters.ChallengeSpecId.NotEmpty();
        var hasTrack = parameters.TrackName.NotEmpty();

        // hack alert
        // for some reason, the Sponsor column in User is the image file, not the sponsor id. to preserve this as an iqueryable (for now)
        // coerce the value to match
        var sponsorId = parameters.SponsorId.NotEmpty() ? $"{parameters.SponsorId}.jpg" : null;

        var baseQuery = _playerStore
            .ListWithNoTracking()
            .Include(p => p.User)
            .Include(p => p.Challenges)
            .Include(p => p.Game)
                .ThenInclude
                (
                    g => g.Challenges
                        .Where(c => hasSpecId ? c.SpecId == parameters.ChallengeSpecId : true)
                        .Where(c => hasSessionStartBegin ? c.StartTime >= parameters.SessionStartWindow.DateStart : true)
                        .Where(c => hasSessionStartEnd ? c.EndTime <= parameters.SessionStartWindow.DateEnd : true)
                )
            .Where(p => p.Game.PlayerMode == PlayerMode.Competition)
            .Where(p => hasGameId ? p.GameId == parameters.GameId : true)
            // note that the database uses the term "competition", but it's "series" in the UI
            .Where(p => hasSeries ? p.Game.Competition == parameters.Series : true)
            // i'm SURE there's a better way to structure this
            .Where(p => hasTrack && parameters.TrackModifier == PlayersReportTrackModifier.CompetedInThisTrack ? p.Game.Track == parameters.TrackName : true)
            .Where(p => hasTrack && parameters.TrackModifier == PlayersReportTrackModifier.DidntCompeteInThisTrack ? p.Game.Track != parameters.TrackName : true)

            // the "competed only in this track" thing is NYI
            //.Where(u => hasTrack && parameters.TrackModifier == PlayersReportTrackModifier.CompetedInOnlyThisTrack ? u.Enrollments.GroupBy(p => p.Game.Track).Count() == 1 : true)
            .AsQueryable()
            .Where(u => sponsorId != null ? u.Sponsor == sponsorId : true);

        return baseQuery;
    }
}
