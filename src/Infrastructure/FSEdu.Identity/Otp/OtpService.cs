using System.Security.Cryptography;
using System.Text;
using FSEdu.Identity.Entities;
using FSEdu.Identity.Persistence;
using FSEdu.Shared.Kernel.Results;
using FSEdu.Shared.Kernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSEdu.Identity.Otp;

public sealed class OtpService : IOtpService
{
    private readonly IdentityDbContext _db;
    private readonly ISmsSender _sms;
    private readonly IDateTimeProvider _clock;
    private const int ExpiryMinutes = 5;
    private const int MaxAttempts = 5;

    public OtpService(IdentityDbContext db, ISmsSender sms, IDateTimeProvider clock)
    {
        _db = db;
        _sms = sms;
        _clock = clock;
    }

    public async Task<Result<string>> GenerateAsync(string phone, CancellationToken ct = default)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        var otp = new OtpCode
        {
            Phone = phone,
            CodeHash = HashCode(code),
            ExpiresAtUtc = _clock.UtcNow.AddMinutes(ExpiryMinutes),
            CreatedAtUtc = _clock.UtcNow
        };

        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync(ct);

        await _sms.SendOtpAsync(phone, code, ct);

        return Result.Success(code);
    }

    public async Task<Result> VerifyAsync(string phone, string code, CancellationToken ct = default)
    {
        var otp = await _db.OtpCodes
            .Where(o => o.Phone == phone && !o.Used)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (otp is null)
            return Error.NotFound("OTP.NOT_FOUND", "لم يتم العثور على رمز تحقق صالح", "No valid OTP found");

        if (otp.ExpiresAtUtc < _clock.UtcNow)
            return Error.Validation("OTP.EXPIRED", "انتهت صلاحية رمز التحقق", "OTP expired");

        if (otp.Attempts >= MaxAttempts)
            return Error.Validation("OTP.TOO_MANY_ATTEMPTS", "عدد محاولات كثيرة، اطلب رمزًا جديدًا", "Too many attempts");

        otp.Attempts++;

        if (HashCode(code) != otp.CodeHash)
        {
            await _db.SaveChangesAsync(ct);
            return Error.Validation("OTP.INVALID", "رمز التحقق غير صحيح", "Invalid OTP");
        }

        otp.Used = true;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string HashCode(string code)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(code + "FSEdu_SALT_v1"));
        return Convert.ToBase64String(bytes);
    }
}

public sealed class ConsoleSmsSender : ISmsSender
{
    private readonly Microsoft.Extensions.Logging.ILogger<ConsoleSmsSender> _logger;

    public ConsoleSmsSender(Microsoft.Extensions.Logging.ILogger<ConsoleSmsSender> logger) => _logger = logger;

    public Task SendOtpAsync(string phone, string code, CancellationToken ct = default)
    {
        _logger.LogWarning("📱 [DEV SMS] Phone: {Phone} | OTP Code: {Code}", phone, code);
        return Task.CompletedTask;
    }
}
