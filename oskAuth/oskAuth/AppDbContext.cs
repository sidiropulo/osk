using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace oskAuth;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<AppUser>();
        user.HasKey(u => u.Id);
        user.HasIndex(u => u.Login).IsUnique();
        user.Property(u => u.Login).HasMaxLength(100);
        user.Property(u => u.DisplayName).HasMaxLength(100);
    }
}

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public required string Login { get; set; }
    [MaxLength(100)]
    public required string DisplayName { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public long AuthVersion { get; set; }
}