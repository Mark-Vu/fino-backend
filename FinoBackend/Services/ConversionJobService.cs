using FinoBackend.Data;
using FinoBackend.Models;
using Microsoft.EntityFrameworkCore;
namespace FinoBackend.Services;

public class ConversionJobService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ConversionJobService> _logger;

    public ConversionJobService(ApplicationDbContext db, ILogger<ConversionJobService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Create new job for a given BankStatementFile
    public async Task<ConversionJob> CreateAsync(Guid bankStatementFileId,
                                                    Guid jobId = default,
                                                    CancellationToken ct = default)
    {
        _logger.LogInformation("Creating conversion job");
        if (jobId == Guid.Empty)
            jobId = Guid.NewGuid();
        
        var job = new ConversionJob
        {
            Id = jobId,
            UploadedFileId = bankStatementFileId,
            Status = JobStatus.Pending,
            ErrorMessage = null,
            StartedAt = DateTime.UtcNow,
            FinishedAt = null
        };

        await _db.ConversionJobs.AddAsync(job, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created ConversionJob {JobId} for file {FileId}", job.Id, bankStatementFileId);
        return job;
    }

    public async Task<ConversionJob?> GetJobByIdAsync(Guid jobId, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting conversion job {JobId}", jobId);
        return await _db.ConversionJobs
            .Include(j => j.UploadedFile)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);
    }
    
    public async Task<List<ConversionJob>> GetJobsByIdsAsync(List<Guid> jobIds, CancellationToken ct = default)
    {
        return await _db.ConversionJobs
            .Include(j => j.UploadedFile)
            .Where(j => jobIds.Contains(j.Id))
            .ToListAsync(ct);
    }

    
    public async Task<bool> UpdateStatusAsync(Guid jobId, JobStatus status, string? errorMessage = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating conversion job");
        var job = await _db.ConversionJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null) return false;

        job.Status = status;
        job.ErrorMessage = errorMessage;

        // If terminal states (Succeeded / Failed), set FinishedAt
        if (status == JobStatus.Success || status == JobStatus.Failed)
            job.FinishedAt = DateTime.UtcNow;
        else
            job.FinishedAt = null;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated ConversionJob {JobId} to {Status}", jobId, status);
        return true;
    }
}