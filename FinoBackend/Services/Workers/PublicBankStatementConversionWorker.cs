using System.Text;
using Tabula;
using Tabula.Detectors;
using Tabula.Extractors;
using UglyToad.PdfPig;

namespace FinoBackend.Services.BankStatementConverter;

public class PublicBankStatementConverter
{
    public async Task<Stream> ConvertPdfToCsvAsync(Stream pdf, CancellationToken ct = default)
    {
        // tabula-sharp needs a seekable stream
        var pdfMem = new MemoryStream();
        await pdf.CopyToAsync(pdfMem, ct);
        pdfMem.Position = 0;

        var latticeCsv = new StringBuilder();
        var streamCsv  = new StringBuilder();

        using var document = PdfDocument.Open(pdfMem, new ParsingOptions { ClipPaths = true });

        for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
        {
            ct.ThrowIfCancellationRequested();
            PageArea page = ObjectExtractor.Extract(document, pageNum);

            // 1) Lattice (grid lines)
            IExtractionAlgorithm lattice = new SpreadsheetExtractionAlgorithm();
            var latticeTables = lattice.Extract(page);
            AppendTablesToCsv(latticeTables, latticeCsv);

            // 2) Stream (whitespace)
            var detector = new SimpleNurminenDetectionAlgorithm();
            var regions  = detector.Detect(page);

            IExtractionAlgorithm basic = new BasicExtractionAlgorithm();

            if (regions.Count == 0)
            {
                var tables = basic.Extract(page); // whole page
                AppendTablesToCsv(tables, streamCsv);
            }
            else
            {
                foreach (var r in regions)
                {
                    var tables = basic.Extract(page.GetArea(r.BoundingBox));
                    AppendTablesToCsv(tables, streamCsv);
                }
            }
        }

        // pick the richer result
        string PickBest(StringBuilder a, StringBuilder b)
        {
            int score(string s) => s.Split('\n').Count(l => !string.IsNullOrWhiteSpace(l));
            var sa = a.ToString();
            var sb = b.ToString();
            return score(sa) >= score(sb) ? sa : sb;
        }

        var best = PickBest(latticeCsv, streamCsv);

        // optional: if nothing extracted, return empty CSV (or throw)
        if (string.IsNullOrWhiteSpace(best))
            best = ""; // or: throw new InvalidOperationException("No table detected (PDF might be scanned).");

        return new MemoryStream(Encoding.UTF8.GetBytes(best)) { Position = 0 };
    }

    private static void AppendTablesToCsv(IReadOnlyList<Table> tables, StringBuilder sb)
    {
        foreach (var table in tables)
        {
            foreach (var row in table.Rows)
            {
                var cells = row.Select(c => CsvEscape(c.GetText()?.Trim() ?? ""));
                if (cells.All(s => s.Length == 0)) continue; // drop blank rows
                sb.AppendLine(string.Join(",", cells));
            }
            // sb.AppendLine(); // uncomment if you want a blank line between tables
        }
    }

    private static string CsvEscape(string v)
    {
        if (v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
