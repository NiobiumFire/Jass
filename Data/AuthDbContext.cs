using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BelotWebApp.Data;

public class AuthDbContext : IdentityDbContext<ApplicationUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {

    }

    public DbSet<UserStats> UserStats { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserStats>(entity =>
        {
            entity.Property(s => s.MatchType).HasConversion<string>();
            entity.Property(s => s.PeriodType).HasConversion<string>();

            entity.HasIndex(s => new { s.UserId, s.MatchType, s.PeriodType, s.PeriodKey }).IsUnique();

            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
