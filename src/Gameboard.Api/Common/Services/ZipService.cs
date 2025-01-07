using System.IO;
using System.IO.Compression;

namespace Gameboard.Api.Common.Services;

public interface IZipService
{
    public ZipArchive Zip(string outputPath, string[] filePaths, string relativeRoot = null);
}

internal sealed class ZipService : IZipService
{
    public ZipArchive Zip(string outputPath, string[] filePaths, string relativeRoot = null)
    {
        using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);

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
