using Microsoft.EntityFrameworkCore;
using HRManagement.API.Models;

namespace HRManagement.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    // ── Tables ────────────────────────────────────────────
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Leave> Leaves => Set<Leave>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Employee → Department ─────────────────────────
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Employee → Manager (auto-référence) ──────────
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Manager)
            .WithMany(e => e.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Contract → Employee ───────────────────────────
        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Employee)
            .WithMany(e => e.Contracts)
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Leave → Employee ──────────────────────────────
        modelBuilder.Entity<Leave>()
            .HasOne(l => l.Employee)
            .WithMany(e => e.Leaves)
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── User → Employee (1-to-1) ──────────────────────
        modelBuilder.Entity<User>()
            .HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<User>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Index unique sur Email ─────────────────────────
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email)
            .IsUnique();

        // ── Index unique sur Username ──────────────────────
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // ── Index unique sur EmployeeId dans Users ────────
        modelBuilder.Entity<User>()
            .HasIndex(u => u.EmployeeId)
            .IsUnique();

        // ── Précision décimaux ────────────────────────────
        modelBuilder.Entity<Employee>()
            .Property(e => e.Salary)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Contract>()
            .Property(c => c.Salary)
            .HasPrecision(10, 2);
    }
}