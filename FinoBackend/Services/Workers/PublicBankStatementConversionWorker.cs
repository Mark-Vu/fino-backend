using System.Text;
using System.Text.RegularExpressions;
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

        var allLatticeTables = new List<Table>();
        var allStreamTables  = new List<Table>();

        using var document = PdfDocument.Open(pdfMem, new ParsingOptions { ClipPaths = true });

        for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
        {
            ct.ThrowIfCancellationRequested();
            var page = ObjectExtractor.Extract(document, pageNum);

            // 1) Lattice (grid lines)
            IExtractionAlgorithm lattice = new SpreadsheetExtractionAlgorithm();
            var latticeTables = lattice.Extract(page);
            if (latticeTables?.Count > 0) allLatticeTables.AddRange(latticeTables);

            // 2) Stream (whitespace)
            var detector = new SimpleNurminenDetectionAlgorithm();
            var regions  = detector.Detect(page);

            IExtractionAlgorithm basic = new BasicExtractionAlgorithm();
            if (regions.Count == 0)
            {
                var tables = basic.Extract(page); // whole page
                if (tables?.Count > 0) allStreamTables.AddRange(tables);
            }
            else
            {
                foreach (var r in regions)
                {
                    var tables = basic.Extract(page.GetArea(r.BoundingBox));
                    if (tables?.Count > 0) allStreamTables.AddRange(tables);
                }
            }
        }

        // Build CSV from each family with header-aware filtering
        var (csvLattice, hasHeaderLattice, dataRowsLattice) = BuildCsvFromTablesPreferHeader(allLatticeTables);
        var (csvStream,  hasHeaderStream,  dataRowsStream)  = BuildCsvFromTablesPreferHeader(allStreamTables);

        string best;
        if (hasHeaderLattice || hasHeaderStream)
        {
            // Prefer the variant where a header was found; break ties by data row count
            if (hasHeaderLattice && hasHeaderStream)
                best = dataRowsLattice >= dataRowsStream ? csvLattice : csvStream;
            else
                best = hasHeaderLattice ? csvLattice : csvStream;
        }
        else
        {
            // Fallback to original "richer result" if no header detected anywhere
            int score(string s) => s.Split('\n').Count(l => !string.IsNullOrWhiteSpace(l));
            best = score(csvLattice) >= score(csvStream) ? csvLattice : csvStream;
        }

        if (string.IsNullOrWhiteSpace(best))
            best = ""; // or throw if you prefer

        return new MemoryStream(Encoding.UTF8.GetBytes(best)) { Position = 0 };
    }

    // --- Helpers ---
    private static (string csv, bool hasHeader, int dataRows) BuildCsvFromTablesPreferHeader(
    IReadOnlyList<Table> tables)
{
    var sb = new StringBuilder();
    var wroteHeader = false;
    var headerKey = "";
    var dataRows = 0;
    int headerColCount = 0;

    bool LooksLikeHeader(IReadOnlyList<string> row)
    {
        var j = string.Join(' ', row).ToLowerInvariant();
        bool hasDate = j.Contains("date");
        bool hasTxn = j.Contains("transaction") || j.Contains("description") ||
                      j.Contains("details") || j.Contains("narration") ||
                      j.Contains("particulars");
        return hasDate && hasTxn;
    }

    string NormalizeForKey(string s) =>
        Regex.Replace((s ?? "").ToLowerInvariant().Trim(), @"\s+", " ");

    string HeaderKey(IReadOnlyList<string> r) =>
        string.Join("|", r.Select(NormalizeForKey));

    bool IsBlankRow(IReadOnlyList<string> row) =>
        row.All(c => string.IsNullOrWhiteSpace(c));

    List<string> CleanRow(List<string> row)
    {
        // normalize "blank" → ""
        var cleaned = row.Select(cell =>
        {
            var text = (cell ?? "").Trim();
            return string.Equals(text, "blank", StringComparison.OrdinalIgnoreCase) ? "" : text;
        }).ToList();

        // trim trailing empty cells
        int lastNonEmpty = cleaned.FindLastIndex(c => !string.IsNullOrWhiteSpace(c));
        return lastNonEmpty >= 0 ? cleaned.Take(lastNonEmpty + 1).ToList() : new List<string>();
    }

    foreach (var table in tables ?? Array.Empty<Table>())
    {
        var rows = table.Rows.Select(r => CleanRow(r.Select(c => c.GetText() ?? "").ToList())).ToList();
        if (rows.Count == 0) continue;

        // find header in first few rows (up to 3)
        var headerIdx = -1;
        var limit = Math.Min(rows.Count, 3);
        for (int i = 0; i < limit; i++)
        {
            if (LooksLikeHeader(rows[i]) && !IsBlankRow(rows[i]))
            {
                headerIdx = i;
                break;
            }
        }

        if (headerIdx < 0)
            continue;

        var header = CleanRow(rows[headerIdx]);
        if (header.Count == 0) continue;

        if (!wroteHeader)
        {
            headerColCount = header.Count; // define schema width
            sb.AppendLine(string.Join(",", header.Select(CsvEscape)));
            wroteHeader = true;
            headerKey = HeaderKey(header);
        }

        for (int i = headerIdx + 1; i < rows.Count; i++)
        {
            var row = CleanRow(rows[i]);
            if (row.Count == 0 || IsBlankRow(row)) continue;

            if (LooksLikeHeader(row) && HeaderKey(row) == headerKey)
                continue;

            // 👉 Normalize row to header column count
            if (row.Count > headerColCount)
                row = row.Take(headerColCount).ToList();
            else if (row.Count < headerColCount)
                row.AddRange(Enumerable.Repeat("", headerColCount - row.Count));

            sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
            dataRows++;
        }
    }

    return (sb.ToString(), wroteHeader, dataRows);
}


    private static string CsvEscape(string v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        return v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
    }
}
