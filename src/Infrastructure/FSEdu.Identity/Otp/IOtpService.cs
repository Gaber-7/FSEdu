using FSEdu.Shared.Kernel.Results;

namespace FSEdu.Identity.Otp;

public interface IOtpService
{
    Task<Result<string>> GenerateAsync(string phone, CancellationToken ct = default);
    Task<Result> VerifyAsync(string phone, string code, CancellationToken ct = default);
}

public interface ISmsSender
{
    Task SendOtpAsync(string phone, string code, CancellationToken ct = default);
}
