using System;

public interface INowService
{
    public DateTimeOffset Get();
}

internal class NowService : INowService
{
    public DateTimeOffset Get() => DateTimeOffset.UtcNow;
}
