using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FinoBackend.Models;

[Table("tenants")]
[Index(nameof(CompanyName), IsUnique = true)]   // 👈 unique company name
[Index(nameof(Subdomain), IsUnique = true)]
public class Tenant : BaseModel
{
    [Required]
    public string CompanyName { get; set; } = string.Empty;   // e.g. "MedipharUsa"

    [Required]
    [MaxLength(100)]
    public string Subdomain { get; set; } = string.Empty; // e.g. "medipharusa"
    
    public ICollection<User> Users { get; set; } = new List<User>();

    // shared files within 1 company
    public ICollection<TenantFile> SharedFiles { get; set; } = new List<TenantFile>();
}