using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Common;

public interface IFileUploadService
{
    Task<IEnumerable<FileUpload>> Upload(string rootDirectory, IEnumerable<IFormFile> files);
}

internal class FileUploadService : IFileUploadService
{
    private static IEnumerable<string> PERMITTED_MIME_TYPES = new string[]
    {
        MediaTypeNames.Image.Gif,
        MediaTypeNames.Image.Jpeg,
        MediaTypeNames.Image.Tiff,
        MediaTypeNames.Text.Plain,

        // some mime types aren't defined in MediaTypeNames
        "image/bmp",
        "image/png",
        "image/svg+xml",
        "image/webp",
        "image/x-png"
    };

    public async Task<IEnumerable<FileUpload>> Upload(string rootDirectory, IEnumerable<IFormFile> files)
    {
        ValidateFileTypes(files);
        var uploads = BuildUploads(files);
        var uploadPath = BuildUploadPath(rootDirectory);
        await WriteFiles(uploadPath, uploads);

        return uploads;
    }

    private void ValidateFileTypes(IEnumerable<IFormFile> files)
    {
        foreach (var file in files)
        {
            if (!PERMITTED_MIME_TYPES.Contains(file.ContentType))
                throw new ProhibitedMimeTypeUploaded(file.Name, file.ContentType, PERMITTED_MIME_TYPES);
        }
    }

    private IEnumerable<FileUpload> BuildUploads(IEnumerable<IFormFile> files)
    {
        var result = new List<FileUpload>();

        if (files == null || files.Count() == 0)
            return result;

        var fileNum = 1;
        foreach (var file in files)
        {
            string nameOnly = Path.GetFileNameWithoutExtension(file.FileName);
            string extension = Path.GetExtension(file.FileName);
            string filename = $"{nameOnly}_{fileNum}{extension}";
            var sanitized = filename.SanitizeFilename().ToLower();
            result.Add(new FileUpload { FileName = sanitized, File = file });
            fileNum += 1;
        }

        return result;
    }

    private string BuildUploadPath(string rootDirectory)
    {
        string path = rootDirectory;

        if (!Directory.Exists(path) && !File.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }

    private async Task WriteFiles(string rootDirectory, IEnumerable<FileUpload> uploads)
    {
        foreach (var upload in uploads)
        {
            string filePath = Path.Combine(rootDirectory, upload.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await upload.File.CopyToAsync(stream);
            }
        }
    }
}

public class FileUpload
{
    public string FileName { get; set; }
    public IFormFile File { get; set; }
}
