using System.Text;
using Tabula;
using Tabula.Detectors;
using Tabula.Extractors;
using UglyToad.PdfPig;

namespace FinoBackend.Services.BankStatementConverter;

public class PublicBankStatementConversionWorker
{
    public async Task<Stream> ConvertPdfToCsvAsync(Stream pdf, CancellationToken ct = default)
    {
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

            // 1) Lattice
            IExtractionAlgorithm lattice = new SpreadsheetExtractionAlgorithm();
            var latticeTables = lattice.Extract(page);
            if (latticeTables?.Count > 0) allLatticeTables.AddRange(latticeTables);

            // 2) Stream
            var detector = new SimpleNurminenDetectionAlgorithm();
            var regions  = detector.Detect(page);

            IExtractionAlgorithm basic = new BasicExtractionAlgorithm();
            if (regions.Count == 0)
            {
                var tables = basic.Extract(page);
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

        var (csvLattice, hasHeaderLattice, dataRowsLattice) = BankStatementCsvHelper.BuildCsvFromTablesPreferHeader(allLatticeTables);
        var (csvStream,  hasHeaderStream,  dataRowsStream)  = BankStatementCsvHelper.BuildCsvFromTablesPreferHeader(allStreamTables);

        string best;
        if (hasHeaderLattice || hasHeaderStream)
        {
            if (hasHeaderLattice && hasHeaderStream)
                best = dataRowsLattice >= dataRowsStream ? csvLattice : csvStream;
            else
                best = hasHeaderLattice ? csvLattice : csvStream;
        }
        else
        {
            int score(string s) => s.Split('\n').Count(l => !string.IsNullOrWhiteSpace(l));
            best = score(csvLattice) >= score(csvStream) ? csvLattice : csvStream;
        }

        if (string.IsNullOrWhiteSpace(best))
            best = "";

        return new MemoryStream(Encoding.UTF8.GetBytes(best)) { Position = 0 };
    }
}
