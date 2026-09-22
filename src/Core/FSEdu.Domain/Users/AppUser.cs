using FSEdu.Domain.Common;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Users;

public class AppUser : AggregateRoot<Guid>, IAuditable, ISoftDelete
{
    public string FullName { get; protected set; } = default!;
    public Email? Email { get; protected set; }
    public PhoneNumber Phone { get; protected set; } = default!;
    public bool PhoneVerified { get; protected set; }
    public bool EmailVerified { get; protected set; }
    public string? WhatsAppNumber { get; protected set; }   // separate contact number; may equal Phone
    public string? AvatarUrl { get; protected set; }
    public string Locale { get; protected set; } = "ar-EG";
    public string Timezone { get; protected set; } = "Africa/Cairo";
    public UserStatus Status { get; protected set; } = UserStatus.Pending;
    public Gender? Gender { get; protected set; }
    public DateOnly? BirthDate { get; protected set; }
    public DateTime? LastLoginAtUtc { get; protected set; }

    // ─── Two-factor (TOTP) ────────────────────────
    public string? TotpSecret { get; protected set; }            // base32 of the shared secret (encrypted at rest at infra layer if needed)
    public bool TotpEnabled { get; protected set; }
    public DateTime? TotpEnabledAtUtc { get; protected set; }
    public string? TotpRecoveryCodesHashed { get; protected set; } // pipe-separated SHA256 hashes of one-time recovery codes

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    protected AppUser() { }

    protected AppUser(Guid id, string fullName, PhoneNumber phone, Email? email = null) : base(id)
    {
        FullName = fullName;
        Phone = phone;
        Email = email;
    }

    public void UpdateProfile(string fullName, string? avatarUrl, Gender? gender, DateOnly? birthDate)
    {
        FullName = fullName;
        AvatarUrl = avatarUrl;
        Gender = gender;
        BirthDate = birthDate;
    }

    public void SetEmail(Email? email)
    {
        // Resets verification when the address actually changes.
        var changed = (Email?.Value ?? "") != (email?.Value ?? "");
        Email = email;
        if (changed) EmailVerified = false;
    }

    public void ChangePhone(PhoneNumber newPhone)
    {
        // Changing the phone number requires a fresh OTP — clear the verified flag.
        var changed = Phone.Value != newPhone.Value;
        Phone = newPhone;
        if (changed) PhoneVerified = false;
    }

    public void SetWhatsAppNumber(string? whatsapp)
    {
        WhatsAppNumber = string.IsNullOrWhiteSpace(whatsapp) ? null : whatsapp.Trim();
    }

    public void VerifyPhone() => PhoneVerified = true;
    public void VerifyEmail() => EmailVerified = true;

    public void Activate() => Status = UserStatus.Active;
    public void Suspend() => Status = UserStatus.Suspended;
    public void Deactivate() => Status = UserStatus.Deactivated;

    public void RecordLogin() => LastLoginAtUtc = DateTime.UtcNow;

    // ─── TOTP lifecycle ──────────────────────────
    // Stores the secret in "pending" state so we don't enable 2FA until the user
    // confirms with a valid code from their authenticator app.
    public void SetTotpSecretPending(string base32Secret)
    {
        TotpSecret = base32Secret;
        TotpEnabled = false;
        TotpEnabledAtUtc = null;
        TotpRecoveryCodesHashed = null;
    }

    public void ConfirmTotp(string hashedRecoveryCodesPipeSeparated)
    {
        if (string.IsNullOrEmpty(TotpSecret))
            throw new InvalidOperationException("TOTP secret not set");
        TotpEnabled = true;
        TotpEnabledAtUtc = DateTime.UtcNow;
        TotpRecoveryCodesHashed = hashedRecoveryCodesPipeSeparated;
    }

    public void DisableTotp()
    {
        TotpSecret = null;
        TotpEnabled = false;
        TotpEnabledAtUtc = null;
        TotpRecoveryCodesHashed = null;
    }

    // Returns the new recovery list (after consuming the matched code), or null if no match
    public string? ConsumeRecoveryCode(string suppliedHash)
    {
        if (string.IsNullOrEmpty(TotpRecoveryCodesHashed)) return null;
        var parts = TotpRecoveryCodesHashed.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (!parts.Remove(suppliedHash)) return null;
        TotpRecoveryCodesHashed = string.Join("|", parts);
        return TotpRecoveryCodesHashed;
    }
}
