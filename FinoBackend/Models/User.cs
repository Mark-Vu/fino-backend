using System.ComponentModel.DataAnnotations.Schema;
using FinoBackend.Commons.Enums;

namespace FinoBackend.Models;

[Table("users")]
public class User : BaseModel
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; }

    // Global role across the entire app
    public GlobalRole GlobalRole { get; set; } = GlobalRole.User;

    // Foreign key to tenant
    [ForeignKey(nameof(Tenant))]
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Tenant-specific role (admin/member)
    public TenantRole? TenantRole { get; set; }

    // Approval flow
    public TenantApprovalStatus? TenantApprovalStatus { get; set; } 
}