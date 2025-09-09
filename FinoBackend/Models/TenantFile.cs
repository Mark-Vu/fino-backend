using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FinoBackend.Models;
[Table("tenant_files")]
public class TenantFile : BaseModel
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }

    public string FileKey { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    public FileExtension FileExtension { get; set; } = FileExtension.Xlsx;
    
    [JsonIgnore]
    public ICollection<ConversionJob> ConversionJobs { get; set; } = new List<ConversionJob>();
}