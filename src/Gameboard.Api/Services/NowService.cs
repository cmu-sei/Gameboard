using System;

namespace Gameboard.Api.Services;

public interface INowService
{
    public DateTimeOffset Get();
}

internal class NowService : INowService
{
    public DateTimeOffset Get() => DateTimeOffset.UtcNow;
}
