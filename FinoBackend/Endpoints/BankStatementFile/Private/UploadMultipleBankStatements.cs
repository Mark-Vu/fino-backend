using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.BankStatementFile;

public class UploadMultipleBankStatement 
    : Endpoint<UploadMultipleBankStatementRequest, UploadMultipleBankStatementResponse>
{
    private readonly ILogger<UploadMultipleBankStatement> _logger;
    private readonly StorageService _storage;

    public UploadMultipleBankStatement(ILogger<UploadMultipleBankStatement> logger, StorageService storage)
    {
        _logger = logger;
        _storage = storage;
    }

    public override void Configure()
    {
        Post("/private/bank-statement-files/upload-multiple");
        Roles("authenticated");
    }

    public override async Task HandleAsync(UploadMultipleBankStatementRequest req, CancellationToken ct)
    {
        _logger.LogInformation("UploadMultipleBankStatement request started {@Req}", req);
        if (req.Count > 10)
        {
            throw new BadRequestException();
        }

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.Parse(sub) != req.UserId)
        {
            throw new UnauthorizedException();
        }

        var uploads = new List<FileUploadDto>();

        for (int i = 0; i < req.Count; i++)
        {
            var (fileId, key, url) = await _storage.CreatePdfUploadPrivateAsync(req.UserId, TimeSpan.FromMinutes(5));
            uploads.Add(new FileUploadDto(fileId, key, url));
            _logger.LogInformation("Generated presigned URL for {UserId}, key={Key}", req.UserId, key);
        }

        await Send.OkAsync(new UploadMultipleBankStatementResponse(uploads), ct);
    }
}

public record UploadMultipleBankStatementRequest([Required] Guid UserId, int Count);

public record UploadMultipleBankStatementResponse(List<FileUploadDto> Files);

public record FileUploadDto(Guid FileId, string FileKey, string UploadUrl);
