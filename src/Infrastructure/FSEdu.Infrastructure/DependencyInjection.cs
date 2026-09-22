using FSEdu.Application.Abstractions;
using FSEdu.Infrastructure.Access;
using FSEdu.Infrastructure.Audit;
using FSEdu.Infrastructure.CurrentUser;
using FSEdu.Infrastructure.Gamification;
using FSEdu.Infrastructure.LiveClassroom;
using FSEdu.Infrastructure.Notifications;
using FSEdu.Infrastructure.Subscriptions;
using FSEdu.Infrastructure.WebPushNotifications;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FSEdu.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IAccessPolicy, AccessPolicy>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IGamificationService, GamificationService>();
        services.AddScoped<IDailyChallengeService, DailyChallengeService>();
        services.AddScoped<IWeeklyDigestService, Parents.WeeklyDigestService>();
        services.AddScoped<IAuditLog, AuditLogService>();

        // Email (SMTP) — no-op when Email:Smtp:Host is unconfigured
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // PDF rendering (certificates)
        services.AddSingleton<ICertificatePdfRenderer, FSEdu.Infrastructure.Pdf.CertificatePdfRenderer>();

        // Web Push (caller must Configure<WebPushOptions> separately)
        services.AddScoped<IWebPushService, WebPushService>();

        // Background: expire stale subscriptions + warn soon-to-expire ones
        services.AddHostedService<SubscriptionExpiryService>();
        // Background: notify enrolled students 10–25 min before their live session
        services.AddHostedService<SessionReminderService>();
        // Background: weekly digest to parents (Saturday 09–11 UTC)
        services.AddHostedService<Parents.WeeklyDigestHostedService>();
        return services;
    }
}
