using FinoBackend.Commons.Enums;
using FinoBackend.Data;
using FinoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace FinoBackend.Services;

public class UploadedFileService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UploadedFileService> _logger;
    
    public UploadedFileService(ApplicationDbContext db, ILogger<UploadedFileService> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<UploadedFile> CreateUploadedFileAsync(
        Guid? userId,
        string fileKey,
        string originalFileName,
        FileCategory fileCategory,
        FileExtension fileExtension,
        Guid? bankStatementFileId = null,
        CancellationToken ct = default)
    {
        var id = bankStatementFileId ?? Guid.NewGuid();

        var file = new UploadedFile
        {
            Id = id,
            UserId = userId,
            OwnerType = userId.HasValue ? OwnerType.AuthenticatedUser : OwnerType.Anonymous,
            Category = fileCategory,
            OriginalFileName = originalFileName,
            UploadedFileKey = fileKey,
            FileExtension = fileExtension,
            OutputFileKey = null,
        };

        _db.UploadedFiles.Add(file);
        await _db.SaveChangesAsync(ct);
        return file;
    }

    

    public async Task<UploadedFile?> GetUploadedFileByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.UploadedFiles
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }
}