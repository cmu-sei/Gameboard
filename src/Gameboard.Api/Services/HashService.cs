using System;
using System.Security.Cryptography;
using System.Text;

namespace Gameboard.Api.Services;

public interface IHashService
{
    string Hash(string input);
}

internal class HashService : IHashService
{
    public string Hash(string input)
    {
        using var sha = SHA512.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }
}
