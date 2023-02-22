using System;

namespace Gameboard.Api.Services;

public interface IGuidService
{
    string GetGuid();
}

internal class GuidService : IGuidService
{
    public string GetGuid()
    {
        return Guid.NewGuid().ToString("n");
    }
}
