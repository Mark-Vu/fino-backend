using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FinoBackend.Data;

namespace FinoBackend.Services.Workers;

/// <summary>
/// Background worker that consumes SQS messages (conversion jobs),
/// downloads PDFs from S3, converts them to CSV, uploads results, 
/// and updates the database.
/// </summary>
public class BankStatementConversionWorker : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly StorageService _storage;
    private readonly ILogger<BankStatementConversionWorker> _logger;
    private readonly BankStatementConverter _bankStatementConverter;

    public BankStatementConversionWorker(
        IAmazonSQS sqs,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        StorageService storage,
        ILogger<BankStatementConversionWorker> logger,
        BankStatementConverter bankStatementConverter)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _config = config;
        _storage = storage;
        _logger = logger;
        _bankStatementConverter = bankStatementConverter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _config["SQS:QueueUrl"]
                       ?? throw new InvalidOperationException("Missing SQS:QueueUrl in configuration");
        _logger.LogInformation("BankStatementConversionWorker started. Listening on {@QueueUrl}", queueUrl);
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 20
            }, stoppingToken);
            if (response.Messages is null || response.Messages.Count < 1) continue;
            foreach (var msg in response.Messages)
            {
                try
                {
                    var jobMessage = JsonSerializer.Deserialize<ConversionJobMessage>(msg.Body);
                    _logger.LogInformation("Job received {@JobMessage}", jobMessage);
                    
                    if (jobMessage is null) continue;
                    
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var job = await db.ConversionJobs
                        .Include(j => j.BankStatementFile)
                        .FirstOrDefaultAsync(j => j.Id == jobMessage.JobId, stoppingToken);
                    
                    if (job is null)
                        continue;

                    job.Status = JobStatus.Processing;
                    job.StartedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);

                    // Convert to CSV (stub)
                    var csvStream = await _bankStatementConverter.ConvertPdfToCsvAsync(job.BankStatementFile.PdfFileKey, stoppingToken);

                    // Upload result to S3
                    var csvKey = _storage.GetPrivateCsvResultKey(job.BankStatementFile.UserId, job.Id);
                    await _storage.UploadAsync(csvKey, csvStream, "text/csv", stoppingToken);

                    // Update DB
                    job.Status = JobStatus.Success;
                    job.FinishedAt = DateTime.UtcNow;
                    job.BankStatementFile.CsvFileKey = csvKey;
                    await db.SaveChangesAsync(stoppingToken);

                    // Remove message from queue
                    await _sqs.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);
                    _logger.LogInformation("Job {@JobMessage} was successfully processed", jobMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Worker error: {ex.Message}");
                    // ⚠️ do not delete message; it will become visible again after VisibilityTimeout
                }
            }
        }
    }
    
}
public record ConversionJobMessage(Guid JobId, Guid FileId, Guid UserId);
