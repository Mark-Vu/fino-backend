using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using FinoBackend.Commons.Enums;
using FinoBackend.Data;
using FinoBackend.Models;
using FinoBackend.Services.DeliveryReceiptConverter.PrivateDeliveryReceiptConverter;

namespace FinoBackend.Services.Workers;

public class PrivateDeliveryReceiptBackgroundWorker : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly StorageService _storage;
    private readonly ILogger<PrivateDeliveryReceiptBackgroundWorker> _logger;
    private readonly PrivateDeliveryReceiptConverter _converter;

    // limit concurrency (max 5 parallel jobs)
    private readonly SemaphoreSlim _semaphore = new(initialCount: 5);

    public PrivateDeliveryReceiptBackgroundWorker(
        IAmazonSQS sqs,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        StorageService storage,
        ILogger<PrivateDeliveryReceiptBackgroundWorker> logger,
        PrivateDeliveryReceiptConverter converter)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _config = config;
        _storage = storage;
        _logger = logger;
        _converter = converter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _config["SQS:DeliveryReceiptConversionQueueUrl"]
                       ?? throw new InvalidOperationException("Missing SQS:DeliveryReceiptConversionQueueUrl in configuration");

        _logger.LogInformation("PrivateDeliveryReceiptBackgroundWorker started. Listening on {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 20
            }, stoppingToken);

            if (response.Messages is null || response.Messages.Count == 0)
                continue;

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
            _logger.LogInformation("Delivery receipt job received {@JobMessage}", jobMessage);

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
                FileExtension.Jpg or FileExtension.Png =>
                    await _converter.ConvertImageToCsvAsync(job.UploadedFile.UploadedFileKey, ct),
                _ => throw new InvalidOperationException($"Unsupported file type: {job.UploadedFile.FileExtension}")
            };

            // Upload result to S3
            var csvKey = StorageKeyBuilder.GetPrivateResultKey(
                jobMessage.UserId,
                jobMessage.JobId,
                FileCategory.DeliveryReceipt);

            await _storage.UploadAsync(csvKey, csvStream, "text/csv", ct);

            // Update DB
            job.Status = JobStatus.Success;
            job.FinishedAt = DateTime.UtcNow;
            job.UploadedFile.OutputFileKey = csvKey;
            await db.SaveChangesAsync(ct);

            // Remove message from queue
            await _sqs.DeleteMessageAsync(queueUrl, msg.ReceiptHandle, ct);

            _logger.LogInformation("Delivery receipt job {@JobMessage} processed successfully", jobMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing delivery receipt message");
            // ⚠️ Consider DLQ handling here
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public record PrivateConversionJobMessage(Guid JobId, Guid FileId, Guid UserId);
