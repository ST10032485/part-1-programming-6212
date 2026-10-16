using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // The two roles are always present
        b.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        b.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = AppRoles.Organiser },
            new Role { RoleId = 2, RoleName = AppRoles.Participant });

        b.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        b.Entity<AppUser>().HasKey(u => u.UserId);


        b.Entity<Category>().HasIndex(c => c.CategoryName).IsUnique();
        b.Entity<Category>().Property(c => c.DistanceKm).HasPrecision(5, 2);

        b.Entity<Event>().HasOne(e => e.Organiser).WithMany().HasForeignKey(e => e.OrganiserId);
        b.Entity<Event>().Property(e => e.DistanceKm).HasPrecision(5, 2);

        b.Entity<EventCategory>().HasIndex(ec => new { ec.EventId, ec.CategoryId }).IsUnique();
        b.Entity<EventCategory>().Property(ec => ec.EntryFee).HasPrecision(10, 2);

        b.Entity<Enrolment>().HasIndex(e => new { e.UserId, e.EventCategoryId }).IsUnique();
        b.Entity<Enrolment>().HasIndex(e => new { e.EventCategoryId, e.BibNumber }).IsUnique();

        // One result per enrolment
        b.Entity<Result>().HasOne(r => r.Enrolment).WithOne(e => e.Result).HasForeignKey<Result>(r => r.EnrolmentId);
        b.Entity<Result>().Property(r => r.FinishTime).HasColumnType("time(0)");

        // No cascade deletes, same as the Part 1 SQL script
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
