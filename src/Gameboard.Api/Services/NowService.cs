using System;

public interface INowService
{
    DateTimeOffset Now();
}

internal class NowService : INowService
{
    public DateTimeOffset Now() => DateTimeOffset.UtcNow;
}
