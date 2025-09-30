// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Common.Services;

public interface IActingUserService
{
    User Get();
}

internal class ActingUserService(
    BackgroundAsyncTaskContext backgroundTaskContext,
    IHttpContextAccessor httpContextAccessor
    ) : IActingUserService
{
    private readonly BackgroundAsyncTaskContext _backgroundTaskContext = backgroundTaskContext;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public User Get()
    {
        return _httpContextAccessor?.HttpContext?.Items[AppConstants.RequestContextGameboardUser] as User ?? _backgroundTaskContext?.ActingUser;
    }
}
