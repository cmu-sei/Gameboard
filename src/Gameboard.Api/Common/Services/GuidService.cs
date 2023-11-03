using System;

namespace Gameboard.Api.Common.Services;

public interface IGuidService
{
    string GetGuid();
}

internal class GuidService : IGuidService
{
    // static so it can be referred to in places where we can't easily inject, like migrations
    public static string StaticGenerateGuid()
    {
        return Guid.NewGuid().ToString("n");
    }

    public string GetGuid()
    {
        return StaticGenerateGuid();
    }
}
