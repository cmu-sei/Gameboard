using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gameboard.Api.Common.Services;

public interface IHtmlToImageService
{
    Task<byte[]> ToPdf(string fileName, string htmlString, int? width = null, int? height = null);

    /// <summary>
    /// Convert a string of HTML to a PNG image. 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="htmlString"></param>
    /// <param name="height">The height of the image to be generated. If left null or if width is set, will infer automatically.</param>
    /// <param name="width">The height of the image to be generated. If left null or if height is set, will infer automatically.</param>
    /// <returns></returns>
    Task<byte[]> ToPng(string fileName, string htmlString, int? width = null, int? height = null);
}

internal class HtmlToImageService : IHtmlToImageService
{
    private readonly CoreOptions _coreOptions;

    public HtmlToImageService(CoreOptions coreOptions)
    {
        _coreOptions = coreOptions;
    }

    public async Task<byte[]> ToPdf(string fileName, string htmlString, int? width = null, int? height = null)
    {
        var tempImageResult = await ToTempImage(fileName, htmlString, width, height);
        var pdfPath = Path.Combine(_coreOptions.TempDirectory, $"{fileName}.pdf");

        // convert the temp image to PDF with chromium headless
        var args = new string[]
        {
            "--headless",
            "--no-sandbox",
            "--disable-gpu",
            "--landscape",
            width != null && height != null ? $"--window-size={width.Value}x{height.Value}" : null,
            $"--print-to-pdf={pdfPath}",
            "--disable-pdf-tagging",
            "--no-pdf-header-footer",
            "--disable-dev-shm-usage",
            tempImageResult.TempImagePath
        }
        .Where(arg => !arg.IsEmpty())
        .ToArray();

        // run chromium and verify
        var result = await StartProcessAsync.StartAsync("chromium", args);
        if (result != 0)
            throw new Exception("PDF generation failed.");

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath);

        tempImageResult.Delete();
        File.Delete(pdfPath);
        return pdfBytes;
    }

    public async Task<byte[]> ToPng(string fileName, string htmlString, int? width = null, int? height = null)
    {
        var tempImageResult = await ToTempImage(fileName, htmlString, width, height);
        var imageBytes = await File.ReadAllBytesAsync(tempImageResult.TempImagePath);

        tempImageResult.Delete();
        return imageBytes;
    }

    private class ToTempImageResult
    {
        public required string TempImagePath { get; set; }
        public required string TempHtmlPath { get; set; }

        public void Delete()
        {
            File.Delete(TempHtmlPath);
            File.Delete(TempImagePath);
        }
    }

    private async Task<ToTempImageResult> ToTempImage(string fileName, string htmlString, int? width = null, int? height = null)
    {
        // create temp paths
        var tempHtmlPath = Path.Combine(_coreOptions.TempDirectory, $"{fileName}.html");
        var tempImagePath = Path.Combine(_coreOptions.TempDirectory, $"{fileName}.png");
        await File.WriteAllTextAsync(tempHtmlPath, htmlString);

        // save it with chromium headless
        var args = new string[]
        {
            "--headless",
            "--no-sandbox",
            "--disable-gpu",
            "--landscape",
            // ask chromium not to use dev shared memory - it defaults to only 64mb on docker
            "--disable-dev-shm-usage",
            width != null && height != null ? $"--window-size={width.Value}x{height.Value}" : null,
            $"--screenshot={tempImagePath}",
            tempHtmlPath
        }
        .Where(arg => !arg.IsEmpty())
        .ToArray();

        // run chromium and verify
        var result = await StartProcessAsync.StartAsync("chromium", args);
        if (result != 0)
            throw new Exception("Image generation failed.");

        // return the temp paths of both files for cleanup later
        return new ToTempImageResult
        {
            TempHtmlPath = tempHtmlPath,
            TempImagePath = tempImagePath
        };
    }
}
