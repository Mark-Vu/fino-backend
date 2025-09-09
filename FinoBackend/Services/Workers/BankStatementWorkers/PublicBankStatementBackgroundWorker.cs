using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FinoBackend.Commons.Enums;
using FinoBackend.Data;
using FinoBackend.Services.BankStatementConverter;

namespace FinoBackend.Services.Workers.BankStatementWorkers;


public class PublicBankStatementBackgroundWorker : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly StorageService _storage;
    private readonly ILogger<PublicBankStatementBackgroundWorker> _logger;
    private readonly PublicBankStatementConverter _publicBankStatementConverter;

    public PublicBankStatementBackgroundWorker(
        IAmazonSQS sqs,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        StorageService storage,
        ILogger<PublicBankStatementBackgroundWorker> logger,
        PublicBankStatementConverter publicBankStatementConverter)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _config = config;
        _storage = storage;
        _logger = logger;
        _publicBankStatementConverter = publicBankStatementConverter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _config["SQS:PublicBankStatementConversionQueueUrl"]
                       ?? throw new InvalidOperationException("Missing SQS:QueueUrl in configuration");
        _logger.LogInformation("PublicBankStatementConversionWorker started. Listening on {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
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
                    var jobMessage = JsonSerializer.Deserialize<PublicConversionJobMessage>(msg.Body);
                    _logger.LogInformation("Job received {@JobMessage}", jobMessage);

                    if (jobMessage is null) continue;

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var job = await db.ConversionJobs
                        .Include(j => j.UploadedFile)
                        .FirstOrDefaultAsync(j => j.Id == jobMessage.JobId, stoppingToken);

                    if (job is null) continue;

                    job.Status = JobStatus.Processing;
                    job.StartedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);

                    // 1) Download PDF from S3
                    var pdfStream = await _storage.DownloadAsync(job.UploadedFile.UploadedFileKey, stoppingToken);

                    // 2) Convert PDF → CSV
                    var csvStream = await _publicBankStatementConverter.ConvertPdfToCsvAsync(pdfStream, stoppingToken);

                    // 3) Upload result to S3
                    var csvKey = StorageKeyBuilder.GetPublicResultKey(jobMessage.JobId, FileCategory.BankStatement);
                    await _storage.UploadAsync(csvKey, csvStream, "text/csv", stoppingToken);

                    // 4) Update DB
                    job.Status = JobStatus.Success;
                    job.FinishedAt = DateTime.UtcNow;
                    job.UploadedFile.OutputFileKey = csvKey;
                    await db.SaveChangesAsync(stoppingToken);

                    // 5) Remove message from queue
                    await _sqs.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, stoppingToken);

                    _logger.LogInformation("Job {@JobMessage} was successfully processed", jobMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker error while processing message");
                }
            }
        }
    }
}

public record PublicConversionJobMessage(Guid JobId, Guid FileId);