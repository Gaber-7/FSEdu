using FSEdu.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Identity.Persistence;

public class IdentityDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.HasDefaultSchema("auth");

        b.Entity<ApplicationUser>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        });

        b.Entity<ApplicationRole>(e => e.ToTable("Roles"));
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>(e => e.ToTable("UserRoles"));
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>(e => e.ToTable("UserClaims"));
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>(e => e.ToTable("UserLogins"));
        b.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>(e => e.ToTable("UserTokens"));
        b.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>(e => e.ToTable("RoleClaims"));

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.UserId);
        });

        b.Entity<OtpCode>(e =>
        {
            e.ToTable("OtpCodes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.Property(x => x.CodeHash).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.Phone, x.Used });
        });
    }
}
