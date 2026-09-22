using Microsoft.AspNetCore.Identity;

namespace FSEdu.Identity.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = default!;
    public Guid? DomainUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }
}

public class RefreshToken
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}

public class OtpCode
{
    public long Id { get; set; }
    public string Phone { get; set; } = default!;
    public string CodeHash { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public int Attempts { get; set; }
    public bool Used { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
