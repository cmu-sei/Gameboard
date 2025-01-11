using System.IO;
using System.IO.Compression;

namespace Gameboard.Api.Common.Services;

public interface IZipService
{
    public ZipArchive Zip(string outputPath, string[] filePaths, string relativeRoot = null);
    public ZipArchive ZipDirectory(string outputPath, string directoryPath);
}

internal sealed class ZipService : IZipService
{
    public ZipArchive ZipDirectory(string outputPath, string directoryPath)
    {
        using var stream = File.Open(outputPath, FileMode.Create, FileAccess.ReadWrite);
        var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        var fullDirectoryPath = Path.GetFullPath(directoryPath);

        foreach (var file in Directory.EnumerateFileSystemEntries(fullDirectoryPath))
        {
            archive.CreateEntryFromFile(file, Path.GetRelativePath(fullDirectoryPath, file));
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
