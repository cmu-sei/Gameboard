using System;

namespace Gameboard.Api.Common.Services;

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
