using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceStack;

namespace Gameboard.Api.Common;

public static class IFormFileExtensions
{
    public static Task<byte[]> ToBytes(this IFormFile file)
        => ToBytes(file, CancellationToken.None);

    public static async Task<byte[]> ToBytes(this IFormFile file, CancellationToken cancellationToken)
    {
        var retVal = Array.Empty<byte>();

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.GetBufferAsBytes();
    }
}
