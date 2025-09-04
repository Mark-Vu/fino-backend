using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.BankStatementFile;

public class UploadBankStatement
    : EndpointWithoutRequest<UploadBankStatementResponse>
{
    private readonly ILogger<UploadBankStatement> _logger;
    private readonly StorageService _storage;

    public UploadBankStatement(ILogger<UploadBankStatement> logger, StorageService storage)
    {
        _logger = logger;
        _storage = storage;
    }

    public override void Configure()
    {
        Post("/public/bank-statement-files/upload");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("UploadBankStatement request started");
        
        var (fileId, key, url) = await _storage.CreatePdfUploadPublicAsync(TimeSpan.FromMinutes(2));
        
        await Send.OkAsync(new UploadBankStatementResponse(
            FileId: fileId,
            FileKey: key,
            UploadUrl: url
        ), ct);
    }
}

public record UploadBankStatementResponse(Guid FileId, string FileKey, string UploadUrl);