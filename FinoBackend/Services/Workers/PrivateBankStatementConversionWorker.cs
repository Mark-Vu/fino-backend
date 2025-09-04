using Amazon.Textract;
using Amazon.Textract.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace FinoBackend.Services.BankStatementConverter;

public class PrivateBankStatementConverter
{
    private readonly IAmazonTextract _textract;
    private readonly IConfiguration _config;
    private readonly ILogger<PrivateBankStatementConverter> _logger;

    public PrivateBankStatementConverter(
        IAmazonTextract textract,
        IConfiguration config,
        ILogger<PrivateBankStatementConverter> logger)
    {
        _textract = textract;
        _config = config;
        _logger = logger;
    }

    public async Task<Stream> ConvertPdfToCsvAsync(string s3Key, CancellationToken ct)
    {
        var bucket = _config["S3:Bucket"]
                     ?? throw new InvalidOperationException("Missing S3:Bucket in config");

        _logger.LogInformation("Starting Textract TABLES analysis for {Bucket}/{Key}", bucket, s3Key);

        // ---- Start TABLES analysis job ----
        var startReq = new StartDocumentAnalysisRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new Amazon.Textract.Model.S3Object
                {
                    Bucket = bucket,
                    Name = s3Key
                }
            },
            FeatureTypes = new List<string> { "TABLES" }
        };

        var startResp = await _textract.StartDocumentAnalysisAsync(startReq, ct);
        var jobId = startResp.JobId!;
        _logger.LogInformation("Textract job started. JobId={JobId}", jobId);

        GetDocumentAnalysisResponse resp;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            resp = await _textract.GetDocumentAnalysisAsync(new GetDocumentAnalysisRequest { JobId = jobId }, ct);
            _logger.LogInformation("Polling TABLES… Status={Status}", resp.JobStatus);
        } while (resp.JobStatus == Amazon.Textract.JobStatus.IN_PROGRESS);

        if (resp.JobStatus != Amazon.Textract.JobStatus.SUCCEEDED)
        {
            _logger.LogError("Textract failed. JobId={JobId}, Status={Status}", jobId, resp.JobStatus);
            throw new Exception($"Textract failed: {resp.JobStatus}");
        }

        // ---- Collect all blocks ----
        var allBlocks = new List<Block>(resp.Blocks);
        while (!string.IsNullOrEmpty(resp.NextToken))
        {
            resp = await _textract.GetDocumentAnalysisAsync(new GetDocumentAnalysisRequest
            {
                JobId = jobId,
                NextToken = resp.NextToken
            }, ct);
            allBlocks.AddRange(resp.Blocks);
        }

        _logger.LogInformation("Collected {Count} blocks", allBlocks.Count);

        var blockMap = allBlocks.ToDictionary(b => b.Id!, b => b);
        var tables = allBlocks.Where(b => b.BlockType == BlockType.TABLE).ToList();
        _logger.LogInformation("Detected {Count} tables", tables.Count);

        var sb = new StringBuilder();

        bool foundFirstHeader = false;
        string? headerKey = null;

        foreach (var table in tables)
        {
            var cellIds = table.Relationships?
                .Where(r => r.Type == RelationshipType.CHILD)
                .SelectMany(r => r.Ids)
                .ToHashSet() ?? new HashSet<string>();

            var cells = allBlocks
                .Where(b => b.BlockType == BlockType.CELL && cellIds.Contains(b.Id));

            var allRows = cells
                .GroupBy(c => c.RowIndex)
                .OrderBy(g => g.Key)
                .Select(g => g.OrderBy(c => c.ColumnIndex).Select(c => GetCellText(c, blockMap)).ToList())
                .ToList();

            if (allRows.Count == 0) continue;

            // --- If we haven’t found the first header yet ---
            if (!foundFirstHeader)
            {
                int headerIndex = allRows.FindIndex(LooksLikeHeader);
                if (headerIndex < 0) continue; // skip until we find one

                foundFirstHeader = true;
                var filteredRows = allRows.Skip(headerIndex).ToList();
                headerKey = RowKey(filteredRows.First());

                foreach (var row in filteredRows)
                {
                    if (row.All(string.IsNullOrWhiteSpace)) continue;
                    sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
                }
            }
            else
            {
                // After first header is found, keep all rows but skip dup headers/empty
                foreach (var row in allRows)
                {
                    if (row.All(string.IsNullOrWhiteSpace)) continue;

                    var rowKey = RowKey(row);
                    if (headerKey != null && rowKey == headerKey) continue; // skip duplicate header

                    sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
                }
            }
        }

        _logger.LogInformation("CSV extraction completed. Lines={Lines}",
            sb.ToString().Split('\n').Count(l => !string.IsNullOrWhiteSpace(l)));

        return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    // ----------------- Helpers -----------------

    private static bool LooksLikeHeader(IReadOnlyList<string> row)
    {
        var j = string.Join(' ', row).ToLowerInvariant();
        bool hasDate = j.Contains("date");
        bool hasTxn = j.Contains("transaction") || j.Contains("description") ||
                      j.Contains("details") || j.Contains("narration") ||
                      j.Contains("particulars");
        return hasDate && hasTxn;
    }

    private static string RowKey(IReadOnlyList<string> row) =>
        string.Join("|", row.Select(s => (s ?? string.Empty).Trim().ToLowerInvariant()));

    private static string GetCellText(Block cell, IDictionary<string, Block> blockMap)
    {
        if (cell.Relationships == null || cell.Relationships.Count == 0)
            return string.Empty;

        var parts = new List<string>();
        foreach (var rel in cell.Relationships)
        {
            if (rel.Type != RelationshipType.CHILD || rel.Ids == null) continue;
            foreach (var id in rel.Ids)
            {
                if (!blockMap.TryGetValue(id, out var child)) continue;
                if (child.BlockType == BlockType.WORD && !string.IsNullOrEmpty(child.Text))
                {
                    parts.Add(child.Text);
                }
                else if (child.BlockType == BlockType.SELECTION_ELEMENT &&
                         child.SelectionStatus == SelectionStatus.SELECTED)
                {
                    parts.Add("☑");
                }
            }
        }
        return Regex.Replace(string.Join(" ", parts), @"\s+", " ").Trim();
    }

    private static string CsvEscape(string v)
    {
        if (string.IsNullOrEmpty(v)) return string.Empty;
        return v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
    }
}
