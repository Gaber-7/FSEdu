namespace FSEdu.Shared.Contracts.Auth;

public sealed record RegisterStudentRequest(
    string FullName,
    string Phone,
    string? Email,
    string Password,
    int StageId,
    int RegionId,
    int? SchoolId,
    string? ParentPhone
);

public sealed record RegisterParentRequest(
    string FullName,
    string Phone,
    string? Email,
    string Password,
    string? NationalId,
    string? Occupation
);

public sealed record RegisterTeacherRequest(
    string FullName,
    string Phone,
    string? Email,
    string Password,
    string? Bio,
    int YearsOfExperience,
    int[] SubjectIds,
    int[] RegionIds,
    TeacherQualificationDto[] Qualifications
);

public sealed record TeacherQualificationDto(
    string Title,
    string Institution,
    int Year,
    string? DocumentUrl
);

public sealed record LoginRequest(string Phone, string Password, string? TotpCode = null);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record SendOtpRequest(string Phone);

public sealed record VerifyOtpRequest(string Phone, string Code);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserSummary User,
    bool TwoFactorRequired = false
);

public sealed record UserSummary(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    string[] Roles,
    string? AvatarUrl = null
);

public sealed record UploadAvatarResponse(string Url);

// ─── Notification preferences ────────────────────
public sealed record NotificationPreferenceDto(
    string Category,       // "subscriptions" | "courses" | "live_sessions" | "qa" | "achievements" | "support" | "announcements"
    string TitleAr,
    string? DescriptionAr,
    string Emoji,
    bool Enabled
);

public sealed record UpdateNotificationPreferenceRequest(string Category, bool Enabled);

public sealed record ErrorResponse(string Code, string Message);

// Self-service student profile edit
public sealed record StudentProfileDto(
    Guid Id,
    string FullName,
    string Phone,
    string? WhatsAppNumber,
    string? Email,
    bool EmailVerified,
    int StageId,
    string StageName,
    int RegionId,
    string RegionName,
    int? SchoolId,
    string? SchoolName,
    string? AvatarUrl,
    int XpPoints,
    int CurrentStreakDays,
    int LongestStreakDays
);

public sealed record UpdateStudentProfileRequest(
    string FullName,
    string? Email,
    string? Phone,
    string? WhatsAppNumber);
