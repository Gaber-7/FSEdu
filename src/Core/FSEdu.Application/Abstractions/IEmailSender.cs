namespace FSEdu.Application.Abstractions;

public interface IEmailSender
{
    // Sends a simple HTML email. Returns false on transport failure (best-effort).
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
