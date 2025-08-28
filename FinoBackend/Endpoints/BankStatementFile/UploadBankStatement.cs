using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.BankStatementFile;

public class UploadBankStatement
    : Endpoint<UploadBankStatementRequest, UploadBankStatementResponse>
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
        Post("/bank-statement-files/upload");
        Roles("authenticated");
    }

    public override async Task HandleAsync(UploadBankStatementRequest req, CancellationToken ct)
    {
        _logger.LogInformation("UploadBankStatement request started {@Req}", req);

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.Parse(sub) != req.UserId)
        {
            throw new UnauthorizedException();
        }

        // one-call helper: create fileId, key, presigned PUT
        var (fileId, key, url) = _storage.CreatePdfUpload(req.UserId, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Generated presigned URL for {UserId}, key={Key}", req.UserId, key);

        await Send.OkAsync(new UploadBankStatementResponse(
            FileId: fileId,
            FileKey: key,
            UploadUrl: url
        ), ct);
    }
}

public record UploadBankStatementRequest([Required] Guid UserId);

public record UploadBankStatementResponse(Guid FileId, string FileKey, string UploadUrl);