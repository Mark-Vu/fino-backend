using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FinoBackend.Commons.Enums;
using FinoBackend.Data;
using FinoBackend.Models;
using FinoBackend.Services.BankStatementConverter;

namespace FinoBackend.Services.Workers.BankStatementWorkers;

public class PrivateBankStatementBackgroundWorker : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly StorageService _storage;
    private readonly ILogger<PrivateBankStatementBackgroundWorker> _logger;
    private readonly PrivateBankStatementConverter _privateBankStatementConverter;

    // limit concurrency (e.g. max 5 parallel jobs at a time)
    private readonly SemaphoreSlim _semaphore = new(initialCount: 5);

    public PrivateBankStatementBackgroundWorker(
        IAmazonSQS sqs,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        StorageService storage,
        ILogger<PrivateBankStatementBackgroundWorker> logger,
        PrivateBankStatementConverter privateBankStatementConverter)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _config = config;
        _storage = storage;
        _logger = logger;
        _privateBankStatementConverter = privateBankStatementConverter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _config["SQS:BankStatementConversionQueueUrl"]
                       ?? throw new InvalidOperationException("Missing SQS:QueueUrl in configuration");
        _logger.LogInformation("PrivateBankStatementConversionWorker started. Listening on {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 20
            }, stoppingToken);

            if (response.Messages is null || response.Messages.Count == 0) continue;

            // process messages concurrently
            var tasks = response.Messages.Select(msg => ProcessMessageAsync(msg, queueUrl, stoppingToken));
            await Task.WhenAll(tasks);
        }
    }

    private async Task ProcessMessageAsync(Message msg, string queueUrl, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var jobMessage = JsonSerializer.Deserialize<PrivateConversionJobMessage>(msg.Body);
            _logger.LogInformation("Job received {@JobMessage}", jobMessage);

            if (jobMessage is null) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var job = await db.ConversionJobs
                .Include(j => j.UploadedFile)
                .FirstOrDefaultAsync(j => j.Id == jobMessage.JobId, ct);

            if (job is null) return;

            job.Status = JobStatus.Processing;
            job.StartedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            // --- Branch by FileExtension ---
            Stream csvStream = job.UploadedFile.FileExtension switch
            {
                FileExtension.Pdf or FileExtension.Tiff =>
                    await _privateBankStatementConverter.ConvertPdfOrTiffToCsvAsync(job.UploadedFile.UploadedFileKey, ct),
                FileExtension.Jpg or FileExtension.Png =>
                    await _privateBankStatementConverter.ConvertImageToCsvAsync(job.UploadedFile.UploadedFileKey, ct),
                _ => throw new InvalidOperationException(
                    $"Unsupported file type: {job.UploadedFile.FileExtension}")
            };

            // Upload result to S3
            var csvKey = StorageKeyBuilder.GetPrivateResultKey(jobMessage.UserId, jobMessage.JobId, FileCategory.BankStatement);
            await _storage.UploadAsync(csvKey, csvStream, "text/csv", ct);

            // Update DB
            job.Status = JobStatus.Success;
            job.FinishedAt = DateTime.UtcNow;
            job.UploadedFile.OutputFileKey = csvKey;
            await db.SaveChangesAsync(ct);

            // Remove message from queue
            await _sqs.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, ct);
            _logger.LogInformation("Job {@JobMessage} was successfully processed", jobMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker error while processing message");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public record PrivateConversionJobMessage(Guid JobId, Guid FileId, Guid UserId);
