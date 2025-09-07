using Amazon.Textract;
using Amazon.Textract.Model;

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

    // PDFs & TIFFs (async Textract)
    public async Task<Stream> ConvertPdfOrTiffToCsvAsync(string s3Key, CancellationToken ct)
    {
        var bucket = _config["S3:Bucket"]
                     ?? throw new InvalidOperationException("Missing S3:Bucket in config");

        _logger.LogInformation("Starting Textract StartDocumentAnalysis for {Bucket}/{Key}", bucket, s3Key);

        var startReq = new StartDocumentAnalysisRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new Amazon.Textract.Model.S3Object { Bucket = bucket, Name = s3Key }
            },
            FeatureTypes = new List<string> { "TABLES" }
        };

        var startResp = await _textract.StartDocumentAnalysisAsync(startReq, ct);
        var jobId = startResp.JobId!;

        GetDocumentAnalysisResponse resp;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            resp = await _textract.GetDocumentAnalysisAsync(new GetDocumentAnalysisRequest { JobId = jobId }, ct);
        } while (resp.JobStatus == Amazon.Textract.JobStatus.IN_PROGRESS);

        if (resp.JobStatus != Amazon.Textract.JobStatus.SUCCEEDED)
            throw new Exception($"Textract failed: {resp.JobStatus}");

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

        return BankStatementCsvHelper.BuildCsvFromBlocks(allBlocks);
    }

    // JPG / PNG (sync Textract)
    public async Task<Stream> ConvertImageToCsvAsync(string s3Key, CancellationToken ct)
    {
        var bucket = _config["S3:Bucket"]
                     ?? throw new InvalidOperationException("Missing S3:Bucket in config");

        _logger.LogInformation("Starting Textract AnalyzeDocument for {Bucket}/{Key}", bucket, s3Key);

        var req = new AnalyzeDocumentRequest
        {
            Document = new Document
            {
                S3Object = new Amazon.Textract.Model.S3Object { Bucket = bucket, Name = s3Key }
            },
            FeatureTypes = new List<string> { "TABLES" }
        };

        var resp = await _textract.AnalyzeDocumentAsync(req, ct);
        return BankStatementCsvHelper.BuildCsvFromBlocks(resp.Blocks);
    }
}
