using System.Text;
using UglyToad.PdfPig;

namespace Hackathon_Docente_2026.Services;

public class PdfExtractionService
{
    public string ExtraerTexto(Stream pdf)
    {
        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(pdf);
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }
}
