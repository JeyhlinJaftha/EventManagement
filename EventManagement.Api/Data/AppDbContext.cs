using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany(u => u.Events)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict deletes to avoid SQL Server "multiple cascade paths" errors
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Participant)
            .WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Event)
            .WithMany(ev => ev.Enrolments)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantId, e.EventId })
            .IsUnique();

        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment)
            .WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId);

        modelBuilder.Entity<Result>().HasIndex(r => r.EnrolmentId).IsUnique();

        modelBuilder.Entity<Event>().Property(e => e.Distance).HasPrecision(8, 2);
    }
}
