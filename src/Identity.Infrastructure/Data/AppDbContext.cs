using Identity.Application.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Helper;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Identity.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService _currentUserService;
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AuditLogArchive> AuditLogArchives { get; set; }


    /// <summary>
    /// On Model Creating
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply soft delete filter globally for all entities implementing ISoftDelete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)),
                    Expression.Constant(false)
                );

                var lambda = Expression.Lambda(body, parameter);

                modelBuilder.Entity(entityType.ClrType)
                            .HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChangesAsync(auditEntries);
        return result;
    }


    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            // Only audit Identity tables
            var tableName = entry.Metadata.GetTableName();
            if (!new[] { "AspNetUsers", "AspNetRoles", "AspNetUserRoles", "AspNetUserClaims" }.Contains(tableName))
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                TableName = tableName,
                Action = entry.State.ToString(),
                UserId = _currentUserService.UserId
            };

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;

                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                if (entry.State == EntityState.Added)
                    auditEntry.NewValues[propertyName] = property.CurrentValue;
                else if (entry.State == EntityState.Deleted)
                    auditEntry.OldValues[propertyName] = property.OriginalValue;
                else if (entry.State == EntityState.Modified && property.IsModified)
                {
                    auditEntry.OldValues[propertyName] = property.OriginalValue;
                    auditEntry.NewValues[propertyName] = property.CurrentValue;
                }

                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                }
            }

            auditEntries.Add(auditEntry);
        }

        // Add to DbSet
        foreach (var auditEntry in auditEntries.Where(a => !a.HasTemporaryProperties))
        {
            AuditLogs.Add(auditEntry.ToAudit());
        }

        return auditEntries.Where(a => a.HasTemporaryProperties).ToList();
    }

    private Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return Task.CompletedTask;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            AuditLogs.Add(auditEntry.ToAudit());
        }

        return SaveChangesAsync();
    }



    // Optional: Add DbSet<T> for other domain entities if needed
}