using System.Net;
using System.Net.Mail;
using FSEdu.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FSEdu.Infrastructure.Notifications;

// SMTP-backed email sender. Configuration (Email:Smtp section):
//   Host, Port, EnableSsl, FromAddress, FromName, Username, Password
// If Host is not configured the sender becomes a no-op (returns false silently).
// This lets dev/local environments run without crashing while still exercising
// the same INotificationService.SendAsync code path.
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _config["Email:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(toEmail))
            return false;

        var port = int.TryParse(_config["Email:Smtp:Port"], out var p) ? p : 587;
        var enableSsl = !bool.TryParse(_config["Email:Smtp:EnableSsl"], out var ssl) || ssl;
        var fromAddress = _config["Email:Smtp:FromAddress"] ?? "no-reply@fsedu.local";
        var fromName = _config["Email:Smtp:FromName"] ?? "FSEdu";
        var user = _config["Email:Smtp:Username"];
        var pass = _config["Email:Smtp:Password"];

        try
        {
            using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
            if (!string.IsNullOrEmpty(user))
                client.Credentials = new NetworkCredential(user, pass ?? "");

            using var msg = new MailMessage();
            msg.From = new MailAddress(fromAddress, fromName);
            msg.To.Add(toEmail);
            msg.Subject = subject;
            msg.IsBodyHtml = true;
            msg.Body = htmlBody;

            await client.SendMailAsync(msg, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP send failed for {To}", toEmail);
            return false;
        }
    }
}

// No-op fallback registered when host config is absent. Keeps the IEmailSender
// dependency satisfied so handlers don't need conditional injection.
public sealed class NoopEmailSender : IEmailSender
{
    public Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        => Task.FromResult(false);
}
