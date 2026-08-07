using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GradLink.API.Data;

public class GradLinkDbContext : IdentityDbContext<ApplicationUser>
{
    public GradLinkDbContext(DbContextOptions<GradLinkDbContext> options) : base(options) { }

    public DbSet<JobListing> JobListings => Set<JobListing>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // JobListing configuration
        builder.Entity<JobListing>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Title).IsRequired().HasMaxLength(200);
            entity.Property(j => j.Description).IsRequired();
            entity.Property(j => j.Location).IsRequired().HasMaxLength(100);
            entity.Property(j => j.Industry).IsRequired().HasMaxLength(100);
            entity.Property(j => j.SalaryRange).HasMaxLength(50);

            entity.HasOne(j => j.Employer)
                .WithMany(u => u.JobListings)
                .HasForeignKey(j => j.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(j => j.Industry);
            entity.HasIndex(j => j.Location);
            entity.HasIndex(j => j.PostedDate);
        });

        // JobApplication configuration
        builder.Entity<JobApplication>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasOne(a => a.JobListing)
                .WithMany(j => j.Applications)
                .HasForeignKey(a => a.JobListingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Graduate)
                .WithMany(u => u.Applications)
                .HasForeignKey(a => a.GraduateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate applications
            entity.HasIndex(a => new { a.JobListingId, a.GraduateId }).IsUnique();
        });

        // Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(500);

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(n => new { n.UserId, n.IsRead });
        });

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.University).HasMaxLength(200);
            entity.Property(u => u.Degree).HasMaxLength(200);
            entity.Property(u => u.CompanyName).HasMaxLength(200);
            entity.Property(u => u.Industry).HasMaxLength(100);
            entity.Property(u => u.Website).HasMaxLength(300);
        });
    }
}
