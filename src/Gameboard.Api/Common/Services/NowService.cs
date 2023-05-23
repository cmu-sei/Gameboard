using System;

namespace Gameboard.Api.Common.Services;

public interface INowService
{
    public DateTimeOffset Get();
}

internal class NowService : INowService
{
    public DateTimeOffset Get() => DateTimeOffset.UtcNow;
}
