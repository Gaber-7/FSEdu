namespace FSEdu.Shared.Contracts.Security;

public sealed record TotpStatusDto(bool Enabled, DateTime? EnabledAtUtc, int RemainingRecoveryCodes);

// Setup phase: server returns the secret + provisioning URI for the QR.
// The client must save and display these once — when the user confirms with
// a valid code, recovery codes are issued.
public sealed record TotpSetupDto(string SecretBase32, string ProvisioningUri, string AccountLabel);

// Returned after enabling or regenerating; plain codes are shown ONCE.
public sealed record TotpEnabledDto(List<string> RecoveryCodes);

public sealed record TotpVerifyRequest(string Code);
