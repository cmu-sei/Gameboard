using System;
using System.Security.Cryptography;

namespace Gameboard.Api.Common.Services;

public interface IRandomService
{
    string GetString(int length, int generatedBytes = 32);
}

public class RandomService : IRandomService
{
    public string GetString(int length, int generatedBytes = 32)
    {
        if (length == 0)
        {
            throw new ArgumentException("Can't generate a random string of length 0.");
        }

        var bytes = RandomNumberGenerator.GetBytes(generatedBytes);
        var randomString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        return Convert.ToBase64String(bytes)
            .Substring(0, Math.Min(randomString.Length, length!));
    }
}
