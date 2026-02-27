using EMI_REMAINDER.Models;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<OtpRecord> OtpRecords => Set<OtpRecord>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Phone).IsUnique();
        });

        // OtpRecords
        modelBuilder.Entity<OtpRecord>(entity =>
        {
            entity.HasIndex(e => e.Phone);
            entity.HasIndex(e => e.RequestId).IsUnique();
        });

        // Bills
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.DueDate);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Bills)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Reminders
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasIndex(e => e.BillId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ReminderDate);
            entity.HasOne(e => e.Bill)
                  .WithMany(b => b.Reminders)
                  .HasForeignKey(e => e.BillId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Reminders)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Payments
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Payments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPreferences
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                  .WithOne(u => u.Preferences)
                  .HasForeignKey<UserPreference>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
                user.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Bill bill)
                bill.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Payment payment)
                payment.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is UserPreference pref)
                pref.UpdatedAt = DateTime.UtcNow;
        }
    }
}
