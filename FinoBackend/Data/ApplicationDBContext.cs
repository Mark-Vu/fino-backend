using FinoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace FinoBackend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UploadedFile> UploadedFiles { get; set; }
    public DbSet<ConversionJob> ConversionJobs { get; set; }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ✅ No more HasPostgresEnum<T>()

        // Just tell EF which column type to use for enums
        modelBuilder.Entity<UploadedFile>()
            .Property(e => e.Category)
            .HasColumnType("file_category");

        modelBuilder.Entity<UploadedFile>()
            .Property(e => e.FileExtension)
            .HasColumnType("file_extension");

        modelBuilder.Entity<UploadedFile>()
            .Property(e => e.OwnerType)
            .HasColumnType("owner_type");

        modelBuilder.Entity<User>()
            .Property(e => e.GlobalRole)
            .HasColumnType("global_role");

    }

    private void UpdateTimestamps()
    {
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
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
