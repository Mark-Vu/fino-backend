using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FinoBackend.Data;
using FinoBackend.Services.BankStatementConverter;
using FinoBackend.Models;

namespace FinoBackend.Services.Workers;

/// <summary>
/// Background worker that consumes SQS messages (conversion jobs),
/// downloads files from S3, converts them to CSV, uploads results, 
/// and updates the database.
/// </summary>
public class PrivateBankStatementConverter : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly StorageService _storage;
    private readonly ILogger<PrivateBankStatementConverter> _logger;
    private readonly BankStatementConverter.PrivateBankStatementConversionWorker _privateBankStatementConversionWorker;

    public PrivateBankStatementConverter(
        IAmazonSQS sqs,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        StorageService storage,
        ILogger<PrivateBankStatementConverter> logger,
        BankStatementConverter.PrivateBankStatementConversionWorker privateBankStatementConversionWorker)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _config = config;
        _storage = storage;
        _logger = logger;
        _privateBankStatementConversionWorker = privateBankStatementConversionWorker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = _config["SQS:QueueUrl"]
                       ?? throw new InvalidOperationException("Missing SQS:QueueUrl in configuration");
        _logger.LogInformation("PrivateBankStatementConversionWorker started. Listening on {@QueueUrl}", queueUrl);

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
                    var jobMessage = JsonSerializer.Deserialize<PrivateConversionJobMessage>(msg.Body);
                    _logger.LogInformation("Job received {@JobMessage}", jobMessage);

                    if (jobMessage is null) continue;

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var job = await db.ConversionJobs
                        .Include(j => j.BankStatementFile)
                        .FirstOrDefaultAsync(j => j.Id == jobMessage.JobId, stoppingToken);

                    if (job is null) continue;

                    job.Status = JobStatus.Processing;
                    job.StartedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);

                    // --- Branch by FileExtension ---
                    Stream csvStream;
                    switch (job.BankStatementFile.FileExtension)
                    {
                        case FileExtension.Pdf:
                        case FileExtension.Tiff:
                            csvStream = await _privateBankStatementConversionWorker
                                .ConvertPdfOrTiffToCsvAsync(job.BankStatementFile.UploadedFileKey, stoppingToken);
                            break;

                        case FileExtension.Jpg:
                        case FileExtension.Png:
                            csvStream = await _privateBankStatementConversionWorker
                                .ConvertImageToCsvAsync(job.BankStatementFile.UploadedFileKey, stoppingToken);
                            break;

                        default:
                            throw new InvalidOperationException(
                                $"Unsupported file type: {job.BankStatementFile.FileExtension}");
                    }

                    // Upload result to S3
                    var csvKey = _storage.GetPublicCsvResultKey(job.Id);
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
                    _logger.LogError(ex, "Worker error while processing message");
                }
            }
        }
    }
}

public record PrivateConversionJobMessage(Guid JobId, Guid FileId, Guid UserId);
