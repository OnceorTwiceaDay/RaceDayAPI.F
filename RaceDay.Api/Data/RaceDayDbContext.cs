using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Code-First EF Core context for RaceDay. Table names, keys and constraints
/// here are deliberately written to mirror RaceDay_Database.sql (the Part 1
/// SQL script) so the Code-First schema and the planning SQL script describe
/// the same design, even though EF's migrations (not the raw .sql file) are
/// what actually create RaceDayDb for Part 2.
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RaceRoute> Routes => Set<RaceRoute>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ----- Users -----
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Role)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.RegisteredDate).HasDefaultValueSql("GETUTCDATE()");
        });

        // ----- Events -----
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Location).HasMaxLength(150).IsRequired();
            entity.Property(e => e.DistanceKm).HasColumnType("decimal(6,2)").IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.OrganisedEvents)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ----- Categories -----
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.CategoryId);
            entity.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.DistanceKm).HasColumnType("decimal(5,2)").IsRequired();
            entity.Property(c => c.EntryFee).HasColumnType("decimal(8,2)");

            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ----- Routes -----
        modelBuilder.Entity<RaceRoute>(entity =>
        {
            entity.ToTable("Routes");
            entity.HasKey(r => r.RouteId);
            entity.Property(r => r.RouteName).HasMaxLength(100).IsRequired();
            entity.Property(r => r.DistanceKm).HasColumnType("decimal(5,2)").IsRequired();
            entity.Property(r => r.StartPoint).HasMaxLength(150).IsRequired();
            entity.Property(r => r.EndPoint).HasMaxLength(150).IsRequired();
            entity.Property(r => r.MapUrl).HasMaxLength(255);

            entity.HasOne(r => r.Event)
                  .WithMany(e => e.Routes)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ----- Enrolments -----
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments");
            entity.HasKey(en => en.EnrolmentId);
            entity.Property(en => en.Status).HasMaxLength(20).IsRequired();
            entity.Property(en => en.PaymentStatus).HasMaxLength(20).IsRequired();
            entity.Property(en => en.EnrolmentDate).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(en => en.Participant)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(en => en.ParticipantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(en => en.Event)
                  .WithMany(e => e.Enrolments)
                  .HasForeignKey(en => en.EventId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(en => en.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(en => en.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // A participant may only hold one active enrolment per event
            // (mirrors UQ_Enrolments_Participant_Event in RaceDay_Database.sql)
            entity.HasIndex(en => new { en.ParticipantId, en.EventId }).IsUnique();
        });

        // ----- Results -----
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(r => r.ResultId);
            entity.Property(r => r.Status).HasMaxLength(20).IsRequired();
            entity.Property(r => r.RecordedDate).HasDefaultValueSql("GETUTCDATE()");

            // One-to-zero-or-one: the FK lives on Result, and is UNIQUE,
            // which is what makes this 1:0..1 instead of 1:many.
            entity.HasOne(r => r.Enrolment)
                  .WithOne(en => en.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.EnrolmentId).IsUnique();
        });
    }
}
