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
        Post("/bank-statement-files/{FileId:guid}/confirm");
        Roles("authenticated");
    }

    public override async Task HandleAsync(UploadBankStatementConfirmRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Confirming Bank Statement File Upload");
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.Parse(sub) != req.UserId)
        {
            throw new UnauthorizedException();
        }

        var key = _storage.GetPrivatePdfUploadKey(req.UserId, req.FileId);

        // Verify S3 object & get metadata via StorageService
        var meta = await _storage.TryHeadAsync(key, ct);
        if (meta is null)
        {
            throw new BadRequestException();
        }

        var file = await _bankStatementService.CreateBankStatementFileAsync(
            id: req.FileId,
            userId: req.UserId,
            pdf_file_key: key,
            csv_file_key: string.Empty,
            ct
            );

        var job = await _conversionJobService.CreateAsync(
            file.Id,
            ct
        );
                    var csvKey = _storage.GetPrivateCsvResultKey(job.BankStatementFile.UserId, job.Id);
        
        
        var response = new UploadBankStatementConfirmResponse(
            Job: job
        );

        var messageToMessageQueue = new ConversionJobMessage(
            JobId: job.Id,
            UserId: req.UserId,
            FileId: file.Id);
        
        await _messageQueueService.EnqueueJobAsync(
            messageToMessageQueue,
            ct
        );
        _logger.LogInformation("Bank Statement Upload File confirmed. Sending conversion job to Message Queue: {@message}", messageToMessageQueue);

        await Send.OkAsync(response, ct);
    }
}

public record UploadBankStatementConfirmRequest(
    [Required]
    Guid UserId,
    [Required]
    Guid FileId);

public record UploadBankStatementConfirmResponse(
     Models.ConversionJob Job
    );

public record ConversionJobMessage(Guid JobId, Guid FileId, Guid UserId);
