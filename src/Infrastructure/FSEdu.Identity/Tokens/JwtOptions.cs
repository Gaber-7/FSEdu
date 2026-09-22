namespace FSEdu.Identity.Tokens;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "FSEdu";
    public string Audience { get; set; } = "FSEdu.Users";
    public string SigningKey { get; set; } = "CHANGE_ME_IN_PROD_AT_LEAST_32_CHARS_LONG_SECRET_KEY!";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
