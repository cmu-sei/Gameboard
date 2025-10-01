// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record ListChallengeTagsQuery() : IRequest<ListChallengeTagsResponse>;

internal sealed class ListChallengeTagsHandler
(
    IPracticeService practiceService
) : IRequestHandler<ListChallengeTagsQuery, ListChallengeTagsResponse>
{
    public async Task<ListChallengeTagsResponse> Handle(ListChallengeTagsQuery request, CancellationToken cancellationToken)
    {
        // NOTE: this is an open endpoint, anon ok

        // challenges can have lots of tags, but the only ones we surface in the practice area are the ones
        // explicitly whitelisted via the "suggested searches" setting of practice area
        var settings = await practiceService.GetSettings(cancellationToken);

        // the challenge spec service's query base screens out practice-ineligible challenges
        // we just pull back all the tags, because they're delimited as a string field
        var query = await practiceService.GetPracticeChallengesQueryBase(includeHiddenChallengesIfHasPermission: false);
        var rawSpecs = await query
            .Select(s => new
            {
                s.Id,
                s.Tags,
            })
            .ToArrayAsync(cancellationToken);

        // now that we're out of the query context, we can create a reverse lookup by tag
        var tagsDict = new Dictionary<string, int>();

        foreach (var spec in rawSpecs)
        {
            var tags = ChallengeSpecMapper
                .StringTagsToEnumerableStringTags(spec.Tags)
                .Where(t => settings.SuggestedSearches.Contains(t));

            foreach (var tag in tags)
            {
                if (!tagsDict.TryAdd(tag, 1))
                {
                    tagsDict[tag] += 1;
                }
            }
        }

        return new ListChallengeTagsResponse
        {
            ChallengeTags = [.. tagsDict.Select(kv => new ListChallengeTagsResponseTag { Tag = kv.Key, ChallengeCount = kv.Value }).OrderBy(t => t.Tag)]
        };
    }
}
