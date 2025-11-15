using GrapheneTrace.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GrapheneTrace.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PatientData> PatientData => Set<PatientData>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ConfigurationSetting> ConfigurationSettings => Set<ConfigurationSetting>();
    public DbSet<ContentTemplate> ContentTemplates => Set<ContentTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasMany(u => u.AssignedPatients)
            .WithOne(u => u.AssignedClinician)
            .HasForeignKey(u => u.AssignedClinicianId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientData>()
            .HasOne(pd => pd.Patient)
            .WithMany(u => u.PatientData)
            .HasForeignKey(pd => pd.PatientId);

        modelBuilder.Entity<Alert>()
            .HasOne(a => a.PatientData)
            .WithMany(pd => pd.Alerts)
            .HasForeignKey(a => a.PatientDataId);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

