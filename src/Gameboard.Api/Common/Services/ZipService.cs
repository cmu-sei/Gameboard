// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Gameboard.Api.Common.Services;

public interface IZipService
{
    public ZipArchive Zip(string outputPath, string[] filePaths, string relativeRoot = null);
    public Task<ZipArchive> ZipDirectory(string outputPath, string directoryPath);
}

internal sealed class ZipService : IZipService
{
    public async Task<ZipArchive> ZipDirectory(string outputPath, string directoryPath)
    {
        using var stream = File.Open(outputPath, FileMode.Create, FileAccess.ReadWrite);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        var fullDirectoryPath = Path.GetFullPath(directoryPath);

        foreach (var file in Directory.GetFiles(fullDirectoryPath, "*", SearchOption.AllDirectories))
        {
            using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
            using var fileReader = new StreamReader(fileStream);
            var entry = archive.CreateEntry(Path.GetRelativePath(fullDirectoryPath, file));
            using var entryStream = entry.Open();
            await fileStream.CopyToAsync(entryStream);
        }

        return archive;
    }

    public ZipArchive Zip(string outputPath, string[] filePaths, string relativeRoot = null)
    {
        using var stream = File.Open(outputPath, FileMode.Create, FileAccess.ReadWrite);
        var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        foreach (var path in filePaths)
        {
            var entryPath = path;
            if (relativeRoot.IsNotEmpty())
            {
                entryPath = Path.GetRelativePath(relativeRoot, path);
            }

            archive.CreateEntryFromFile(path, entryPath);
        }

        return archive;
    }
}
