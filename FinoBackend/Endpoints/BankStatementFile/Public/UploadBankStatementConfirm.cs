using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.BankStatementFile;

// After frontend uploads the PDF to S3, confirm & insert DB row
public class UploadBankStatementConfirm : Endpoint<UploadBankStatementConfirmRequest, UploadBankStatementConfirmResponse>
{
    private readonly StorageService _storage;
    private readonly BankStatementService _bankStatementService;
    private readonly ConversionJobService _conversionJobService;
    private readonly MessageQueueService _messageQueueService;
    private readonly ILogger<UploadBankStatementConfirm> _logger;

    public UploadBankStatementConfirm(
        ConversionJobService conversionJobService, 
        StorageService storage, 
        BankStatementService bankStatementService,
        MessageQueueService messageQueueService,
        ILogger<UploadBankStatementConfirm> logger)
    {
        _conversionJobService = conversionJobService;
        _storage = storage;
        _bankStatementService = bankStatementService;
        _messageQueueService = messageQueueService;
        _logger = logger;
    }

    public override void Configure()
    {
        // name parameter to match DTO property for binding
        Post("/public/bank-statement-files/{FileId:guid}/confirm");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UploadBankStatementConfirmRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Confirming Bank Statement File Upload");

        var key = _storage.GetPublicUploadKey(req.FileId);
        var jobId = Guid.NewGuid();

        var validation = await _storage.ValidateFileAsync(key, ct);
        if (!validation.Exists)
            throw new BadRequestException("File not found in storage.");

        if (!validation.ValidFileSize)
            throw new BadRequestException($"File exceeds max size of 50MB. Actual size: {validation.size / (1024*1024)} MB");

        var messageToMessageQueue = new ConversionJobMessage( 
            JobId: jobId, FileId: req.FileId, UserId:null);

        var file = await _bankStatementService.CreateBankStatementFileAsync(
            userId: null,
            fileKey: key,
            bankStatementFileId: req.FileId,
            OriginalFileName:  req.FileName,
            ct: ct);

        var job = await _conversionJobService.CreateAsync(
            bankStatementFileId: file.Id,
            jobId: jobId,
            ct
        );
        var csvKey = _storage.GetPublicCsvResultKey(job.Id);
        
        
        var response = new UploadBankStatementConfirmResponse(
            Job: job
        );
        
        var queueUrl = await _messageQueueService.EnqueueJobAsync(
            messageToMessageQueue,
            isPublic: true,
            ct
        );
        _logger.LogInformation("Enqueued {@message} in {@QueueUrl}", messageToMessageQueue, queueUrl);

        await Send.OkAsync(response, ct);
    }
}

public record UploadBankStatementConfirmRequest(
    [Required]
    Guid FileId,
    [Required]
    string FileName
    );

public record UploadBankStatementConfirmResponse(
     Models.ConversionJob Job
    );

public record ConversionJobMessage(Guid JobId, Guid FileId, Guid? UserId);
