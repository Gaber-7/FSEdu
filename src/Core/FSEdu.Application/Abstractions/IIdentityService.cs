using FSEdu.Shared.Kernel.Results;

namespace FSEdu.Application.Abstractions;

public interface IIdentityService
{
    Task<Result<Guid>> CreateUserAsync(string phone, string? email, string fullName, string password,
                                         string role, CancellationToken ct = default);

    Task<Result<AuthTokens>> SignInAsync(string phone, string password, CancellationToken ct = default);

    // Phase 1 of 2FA login: verify password only and return the user id.
    Task<Result<Guid>> VerifyPasswordAsync(string phone, string password, CancellationToken ct = default);

    // Phase 2 of 2FA login: issue tokens for an already-authenticated user.
    Task<Result<AuthTokens>> IssueTokensForUserAsync(Guid userId, CancellationToken ct = default);

    Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken ct = default);

    Task<Result> SendOtpAsync(string phone, CancellationToken ct = default);

    Task<Result> VerifyOtpAsync(string phone, string code, CancellationToken ct = default);

    Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken ct = default);
}

public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string FullName,
    string Phone,
    string? Email,
    string[] Roles,
    string? AvatarUrl = null);
