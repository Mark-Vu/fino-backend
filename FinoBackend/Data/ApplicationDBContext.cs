using Microsoft.EntityFrameworkCore;
using FinoBackend.Models;

namespace FinoBackend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<BankStatementFile> BankStatementFiles { get; set; }
    public DbSet<ConversionJob> ConversionJobs { get; set; }

    // Override SaveChanges to handle CreatedAt / UpdatedAt
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        // auto update createdAt, updatedAt and generate ID
        var entries = ChangeTracker.Entries<BaseModel>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty) entry.Entity.Id = Guid.NewGuid();
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                // Don’t overwrite CreatedAt when updating
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}