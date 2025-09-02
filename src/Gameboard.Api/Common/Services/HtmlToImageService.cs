using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

internal class HtmlToImageService(CoreOptions coreOptions, ILogger<HtmlToImageService> logger) : IHtmlToImageService
{

    public async Task<byte[]> ToPdf(string fileName, string htmlString, int? width = null, int? height = null)
    {
        var tempHtmlPath = Path.Combine(coreOptions.TempDirectory, $"{fileName}.html");
        var pdfPath = Path.Combine(coreOptions.TempDirectory, $"{fileName}.pdf");
        await File.WriteAllTextAsync(tempHtmlPath, htmlString);

        var args = new string[]
        {
            "--title",
            """ "Gameboard Certificate" '""",
            "--margin-top",
            "0mm",
            "--margin-right",
            "0mm",
            "--margin-bottom",
            "0mm",
            "--margin-left",
            "0mm",
            "-O",
            "Landscape",
            "--page-size",
            "Letter",
            "--no-outline",
            tempHtmlPath,
            pdfPath
        };

        // run chromium and verify
        logger.LogInformation("Starting wkhtmltopdf with args {args}", string.Join(' ', args));
        var result = await StartProcessAsync.StartAsync("wkhtmltopdf", args);
        if (result != 0)
            throw new Exception("PDF generation failed.");

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath);

        File.Delete(tempHtmlPath);
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
        var tempHtmlPath = Path.Combine(coreOptions.TempDirectory, $"{fileName}.html");
        var tempImagePath = Path.Combine(coreOptions.TempDirectory, $"{fileName}.png");
        await File.WriteAllTextAsync(tempHtmlPath, htmlString);

        // save it with wkhtmltoimage
        var args = new string[]
        {
            "-f",
            "png",
            "--quality",
            "30",
            tempHtmlPath,
            tempImagePath
        };

        logger.LogInformation("Starting wkhtmltopdf with args {args}", string.Join(' ', args));
        var result = await StartProcessAsync.StartAsync("wkhtmltoimage", args);
        if (result != 0)
        {
            throw new Exception("Image generation failed.");
        }

        // return the temp paths of both files for cleanup later
        return new ToTempImageResult
        {
            TempHtmlPath = tempHtmlPath,
            TempImagePath = tempImagePath
        };
    }
}
