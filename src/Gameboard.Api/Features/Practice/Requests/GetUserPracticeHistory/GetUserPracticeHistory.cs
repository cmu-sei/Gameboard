// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record GetUserPracticeHistoryQuery(string UserId) : IRequest<UserPracticeHistoryChallenge[]>;

internal sealed class GetUserPracticeHistoryHandler(IPracticeService practice, IValidatorService validator) : IRequestHandler<GetUserPracticeHistoryQuery, UserPracticeHistoryChallenge[]>
{
    public async Task<UserPracticeHistoryChallenge[]> Handle(GetUserPracticeHistoryQuery request, CancellationToken cancellationToken)
    {
        await validator.Auth(c =>
            c.RequireOneOf(PermissionKey.Admin_View)
            .UnlessUserIdIn(request.UserId)
        )
        .Validate(cancellationToken);

        return await practice.GetUserPracticeHistory(request.UserId, cancellationToken);
    }
}
