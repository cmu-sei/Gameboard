// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Common.Services;

public interface IImageStoreService
{
    bool ImageExists(string fileName);
    Task<string> SaveImage(IFormFile file, ImageStoreType imageStoreType, string saveAsFileName, CancellationToken cancellationToken, bool throwOnExists = false);
}

internal sealed class ImageStoreService(CoreOptions coreOptions) : IImageStoreService
{
    public bool ImageExists(string fileName)
    {
        var path = Path.Combine(coreOptions.ImageFolder, fileName);
        return File.Exists(path);
    }

    public async Task<string> SaveImage(IFormFile file, ImageStoreType imageStoreType, string saveAsFileName, CancellationToken cancellationToken, bool throwOnExists = false)
    {
        var extension = Path.GetExtension(file.FileName);
        var imageStoreTypeDirectory = Path.Combine(coreOptions.ImageFolder, imageStoreType.ToString());
        var finalFileName = $"{saveAsFileName}{extension}";
        var path = Path.Combine(imageStoreTypeDirectory, finalFileName);

        if (throwOnExists && File.Exists(path))
        {
            throw new GameboardException($"""Image "{path}" already exists.""");
        }

        if (!Directory.Exists(imageStoreTypeDirectory))
        {
            Directory.CreateDirectory(imageStoreTypeDirectory);
        }

        using var stream = new FileStream(path, FileMode.OpenOrCreate);
        await stream.WriteAsync(await file.ToBytes(), cancellationToken);


        // the final returned value is relative to the root image directory
        return Path.GetRelativePath(coreOptions.ImageFolder, path);
    }
}
