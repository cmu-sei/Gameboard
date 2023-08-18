using System.Threading.Tasks;
using SelectPdf;

namespace Gameboard.Api.Common.Services;

public interface IHtmlToPdfService
{
    Task<byte[]> ToPdf(string html);
}

internal class HtmlToPdfService : IHtmlToPdfService
{
    private readonly CoreOptions _coreOptions;

    public HtmlToPdfService(CoreOptions coreOptions)
    {
        _coreOptions = coreOptions;
    }

    public Task<byte[]> ToPdf(string html)
    {
        var converter = new HtmlToPdf();
        converter.Options.PdfPageSize = PdfPageSize.A4;
        converter.Options.PdfPageOrientation = PdfPageOrientation.Landscape;
        converter.Options.WebPageWidth = 3300;
        converter.Options.WebPageHeight = 2550;

        var doc = converter.ConvertHtmlString(html);
        var bytes = doc.Save();

        doc.Close();
        return Task.FromResult(bytes);
    }
}
