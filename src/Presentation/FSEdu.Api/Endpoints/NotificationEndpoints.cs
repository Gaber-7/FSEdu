using FSEdu.Application.Abstractions;
using FSEdu.Application.Features.Notifications;
using FSEdu.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Api.Endpoints;

public sealed record PushSubscribeRequest(string Endpoint, PushSubscribeKeys Keys);
public sealed record PushSubscribeKeys(string P256dh, string Auth);

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/my", async (int? take, bool? unreadOnly, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyNotificationsQuery(take ?? 30, unreadOnly ?? false), ct)).ToHttpResult());

        group.MapGet("/unread-count", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetUnreadCountQuery(), ct)).ToHttpResult());

        group.MapPost("/{id:guid}/read", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new MarkNotificationReadCommand(id), ct)).ToHttpResult());

        group.MapPost("/read-all", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new MarkAllNotificationsReadCommand(), ct)).ToHttpResult());

        // ─── Preferences ──────────────────────────────
        group.MapGet("/preferences", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyNotificationPreferencesQuery(), ct)).ToHttpResult());

        group.MapPut("/preferences", async (
            FSEdu.Shared.Contracts.Auth.UpdateNotificationPreferenceRequest req,
            ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateNotificationPreferenceCommand(req.Category, req.Enabled), ct)).ToHttpResult());

        // ─── Web Push subscription management ──────────────
        var push = app.MapGroup("/api/v1/push").WithTags("Push");

        // Public endpoint — browser needs the VAPID key to subscribe.
        push.MapGet("/vapid-key", (IWebPushService svc) =>
            Results.Ok(new { publicKey = svc.GetPublicKey(), enabled = svc.IsConfigured }));

        push.MapPost("/subscribe",
            async (PushSubscribeRequest req, HttpContext ctx, IApplicationDbContext db,
                   ICurrentUser currentUser, CancellationToken ct) =>
            {
                if (currentUser.UserId is null) return Results.Unauthorized();
                if (string.IsNullOrEmpty(req.Endpoint) || string.IsNullOrEmpty(req.Keys?.P256dh) || string.IsNullOrEmpty(req.Keys?.Auth))
                    return Results.BadRequest();

                // Idempotent — replace any existing record for the same endpoint.
                var existing = await db.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.Endpoint == req.Endpoint, ct);
                if (existing is not null) db.PushSubscriptions.Remove(existing);

                var ua = ctx.Request.Headers.UserAgent.ToString();
                if (ua.Length > 500) ua = ua.Substring(0, 500);
                db.PushSubscriptions.Add(new PushSubscription(
                    Guid.NewGuid(), currentUser.UserId.Value,
                    req.Endpoint, req.Keys.P256dh, req.Keys.Auth,
                    string.IsNullOrEmpty(ua) ? null : ua));
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { ok = true });
            })
           .RequireAuthorization();

        push.MapPost("/unsubscribe",
            async (PushSubscribeRequest req, IApplicationDbContext db,
                   ICurrentUser currentUser, CancellationToken ct) =>
            {
                if (currentUser.UserId is null) return Results.Unauthorized();
                var existing = await db.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.Endpoint == req.Endpoint && s.UserId == currentUser.UserId, ct);
                if (existing is not null)
                {
                    db.PushSubscriptions.Remove(existing);
                    await db.SaveChangesAsync(ct);
                }
                return Results.Ok();
            })
           .RequireAuthorization();

        return app;
    }
}
