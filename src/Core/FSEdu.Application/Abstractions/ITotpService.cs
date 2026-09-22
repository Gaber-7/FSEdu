namespace FSEdu.Application.Abstractions;

public interface ITotpService
{
    // Generates a fresh secret (160-bit) returned as base32 string for storage and provisioning URI.
    string GenerateSecretBase32();

    // Builds an otpauth:// provisioning URI for QR display.
    // accountLabel = "FSEdu:user@example.com" (issuer:account)
    string BuildProvisioningUri(string issuer, string accountLabel, string secretBase32);

    // Validates a 6-digit code against a base32 secret within +/- 1 30-sec window.
    bool VerifyCode(string secretBase32, string code);

    // Returns N plain recovery codes (e.g. "ABCD-EFGH-IJKL") for user to save once,
    // plus their SHA256 hashed pipe-joined form for persistence.
    (List<string> Plain, string HashedJoined) GenerateRecoveryCodes(int count = 10);

    // SHA256 of the recovery code (uppercase, hyphens stripped).
    string HashRecoveryCode(string code);
}
