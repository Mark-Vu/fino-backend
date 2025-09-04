using Amazon.Textract;
using Amazon.Textract.Model;
using Microsoft.Extensions.Logging;
using System.Text;

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

        _logger.LogInformation("Starting Textract analysis for Bucket={Bucket}, Key={Key}", bucket, s3Key);

        // 1) Start async analysis
        var startReq = new StartDocumentAnalysisRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new Amazon.Textract.Model.S3Object { Bucket = bucket, Name = s3Key }
            },
            FeatureTypes = new List<string> { "TABLES" }
        };

        var startResp = await _textract.StartDocumentAnalysisAsync(startReq, ct);
        var jobId = startResp.JobId;
        _logger.LogInformation("Textract job started. JobId={JobId}", jobId);

        // 2) Poll for completion
        GetDocumentAnalysisResponse firstResp;
        int polls = 0;
        var startedAt = DateTime.UtcNow;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            polls++;
            firstResp = await _textract.GetDocumentAnalysisAsync(new GetDocumentAnalysisRequest { JobId = jobId }, ct);
            _logger.LogInformation("Polling Textract… Attempt={Polls}, Status={Status}", polls, firstResp.JobStatus);
        }
        while (firstResp.JobStatus == Amazon.Textract.JobStatus.IN_PROGRESS);

        if (firstResp.JobStatus != Amazon.Textract.JobStatus.SUCCEEDED)
        {
            _logger.LogError("Textract failed. JobId={JobId}, Status={Status}", jobId, firstResp.JobStatus);
            throw new Exception($"Textract failed: {firstResp.JobStatus}");
        }

        _logger.LogInformation("Textract SUCCEEDED in {Secs:F1}s. First page blocks={Count}",
            (DateTime.UtcNow - startedAt).TotalSeconds, firstResp.Blocks.Count);

        // 3) Collect ALL pages via NextToken
        var allBlocks = new List<Block>(firstResp.Blocks);
        var next = firstResp.NextToken;
        while (!string.IsNullOrEmpty(next))
        {
            var pageResp = await _textract.GetDocumentAnalysisAsync(new GetDocumentAnalysisRequest
            {
                JobId = jobId,
                NextToken = next
            }, ct);
            allBlocks.AddRange(pageResp.Blocks);
            next = pageResp.NextToken;
            _logger.LogInformation("Fetched more blocks: {Added} (total {Total})", pageResp.Blocks.Count, allBlocks.Count);
        }
        _logger.LogInformation("Total blocks collected: {Total}", allBlocks.Count);

        // Build a map for relationship lookups
        var blockMap = allBlocks.ToDictionary(b => b.Id!, b => b);

        var cells = allBlocks.Where(b => b.BlockType == BlockType.CELL);

        // ---- Step 4: Extract raw rows ----
        var rawRows = new List<List<string>>();
        foreach (var pageGroup in cells.GroupBy(c => c.Page).OrderBy(g => g.Key))
        {
            foreach (var rowGroup in pageGroup.GroupBy(c => c.RowIndex).OrderBy(g => g.Key))
            {
                var ordered = rowGroup.OrderBy(c => c.ColumnIndex).ToList();
                var values = new List<string>(ordered.Count);
                foreach (var cell in ordered)
                    values.Add(GetCellText(cell, blockMap));
                rawRows.Add(values);
            }
        }
        _logger.LogInformation("Raw rows extracted: {Count}", rawRows.Count);

        // ---- Step 5: Filtering/deduplication ----
        bool LooksLikeHeader(IReadOnlyList<string> row)
        {
            var joined = string.Join(" ", row).ToLowerInvariant();
            return joined.Contains("date") && joined.Contains("transaction");
        }

        var dateRegex = new System.Text.RegularExpressions.Regex(
            @"^(\d{4}[-/])?\d{1,2}([-/]\d{1,2}([-/]\d{2,4})?|\s+[A-Za-z]{3})$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        int headerIndex = rawRows.FindIndex(r => r.Count > 0 && LooksLikeHeader(r));
        if (headerIndex < 0)
        {
            _logger.LogWarning("No header row detected; writing all rows unfiltered.");
            headerIndex = 0;
        }

        var header = rawRows[headerIndex];
        string HeaderKey(IReadOnlyList<string> r) =>
            string.Join("|", r.Select(s => System.Text.RegularExpressions.Regex.Replace((s ?? "").ToLowerInvariant().Trim(), @"\s+", " ")));
        var headerKey = HeaderKey(header);

        var sb = new StringBuilder();

        // Write header once
        sb.AppendLine(string.Join(",", header.Select(CsvEscape)));

        // Process rows after header
        for (int i = headerIndex + 1; i < rawRows.Count; i++)
        {
            var row = rawRows[i];
            if (row.Count == 0) continue;

            // skip duplicate headers
            if (LooksLikeHeader(row) && HeaderKey(row) == headerKey)
                continue;

            var first = (row[0] ?? "").Trim();

            // keep only rows whose first cell looks like a date
            if (!dateRegex.IsMatch(first))
                continue;

            sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
        }

        _logger.LogInformation("Filtered CSV rows: {Rows}", sb.ToString().Split('\n').Count(l => !string.IsNullOrWhiteSpace(l)));

        return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
    }

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
        return System.Text.RegularExpressions.Regex.Replace(string.Join(" ", parts), @"\s+", " ").Trim();
    }

    private static string CsvEscape(string v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        if (v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
