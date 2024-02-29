using System.Collections.Generic;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Features.Admin;

public record GetAppActiveTeamsResponse(IEnumerable<Team> Teams);
