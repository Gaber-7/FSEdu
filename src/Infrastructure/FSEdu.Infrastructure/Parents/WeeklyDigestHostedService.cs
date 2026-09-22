using FSEdu.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSEdu.Infrastructure.Parents;

// Wakes up every hour. Sends weekly digests to parents whose last send was
// >= 7 days ago, but only during the "digest window" (default: Saturday 09:00–11:00 UTC)
// so we don't email at random odd hours. The cadence check on each parent prevents
// duplicate sends in case the service runs multiple times within the window.
public sealed class WeeklyDigestHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WeeklyDigestHostedService> _logger;
    private static readonly TimeSpan Tick = TimeSpan.FromHours(1);

    public WeeklyDigestHostedService(IServiceProvider services, ILogger<WeeklyDigestHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small startup delay so app init can finish
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now.DayOfWeek == DayOfWeek.Saturday && now.Hour is >= 9 and <= 11)
                {
                    using var scope = _services.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IWeeklyDigestService>();
                    var sent = await svc.SendDueDigestsAsync(stoppingToken);
                    if (sent > 0) _logger.LogInformation("Weekly digest: sent {Sent} emails", sent);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "WeeklyDigestHostedService tick failed"); }

            try { await Task.Delay(Tick, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
