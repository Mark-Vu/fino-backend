// Models/ConversionJob.cs
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FinoBackend.Models;

[Table("conversion_jobs")]
public class ConversionJob : BaseModel
{
    public JobStatus Status { get; set; } = JobStatus.Pending;

    public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(UploadedFile))]
    public Guid UploadedFileId { get; set; }

    [JsonIgnore]
    public UploadedFile UploadedFile { get; set; } = null!;

    // Set these in code (worker/service), not with property initializers.
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}