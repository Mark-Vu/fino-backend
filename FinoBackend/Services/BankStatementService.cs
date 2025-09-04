using FinoBackend.Data;
using FinoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace FinoBackend.Services;

public class BankStatementService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BankStatementService> _logger;
    
    public BankStatementService(ApplicationDbContext db, ILogger<BankStatementService> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<BankStatementFile> CreateBankStatementFileAsync(
        Guid? userId,
        string pdfFileKey,
        string OriginalFileName,
        Guid? bankStatementFileId = null,
        CancellationToken ct = default)
    {
        var id = bankStatementFileId ?? Guid.NewGuid();

        var file = new BankStatementFile
        {
            Id = id,
            UserId = userId,
            OwnerType = userId.HasValue ? OwnerType.AuthenticatedUser : OwnerType.Anonymous,
            OriginalFileName = OriginalFileName,
            PdfFileKey = pdfFileKey,
            CsvFileKey = null,
        };

        _db.BankStatementFiles.Add(file);
        await _db.SaveChangesAsync(ct);
        return file;
    }

    

    public async Task<BankStatementFile?> GetBankStatementFileByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.BankStatementFiles
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }
}