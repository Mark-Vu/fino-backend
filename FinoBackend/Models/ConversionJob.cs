// Models/ConversionJob.cs
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using FinoBackend.Commons.Enums;

namespace FinoBackend.Models;

[Table("conversion_jobs")]
public class ConversionJob : BaseModel
{
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? ErrorMessage { get; set; }

    // Public flow
    [ForeignKey(nameof(UploadedFile))]
    public Guid? UploadedFileId { get; set; }
    public UploadedFile? UploadedFile { get; set; }

    // Tenant flow
    [ForeignKey(nameof(TenantFile))]
    public Guid? TenantFileId { get; set; }
    public TenantFile? TenantFile { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}