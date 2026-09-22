using FSEdu.Api.LiveKit;
using FSEdu.Application.Abstractions;
using FSEdu.Application.Features.LiveClassroom;
using FSEdu.Shared.Contracts.LiveClassroom;
using FSEdu.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSEdu.Api.Endpoints;

public static class LiveClassroomEndpoints
{
    public static IEndpointRouteBuilder MapLiveClassroomEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Teacher ───────────────────────────────
        var teacher = app.MapGroup("/api/v1/teacher/live")
            .WithTags("Teacher.Live")
            .RequireAuthorization(p => p.RequireRole("Teacher"));

        teacher.MapPost("/", async (ScheduleLiveSessionRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ScheduleLiveSessionCommand(
                req.SubjectId, req.StageId, req.Title, req.Description,
                req.ScheduledAtUtc, req.DurationMinutes), ct);
            return result.ToHttpResult();
        });

        teacher.MapPost("/bulk", async (BulkScheduleBody req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new BulkScheduleLiveSessionsCommand(
                req.SubjectId, req.StageId, req.TitlePattern, req.Description,
                req.FirstStartUtc, req.DurationMinutes, req.Occurrences), ct);
            return result.ToHttpResult();
        });

        teacher.MapGet("/my", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyLiveSessionsQuery(), ct)).ToHttpResult());

        teacher.MapPost("/{id:guid}/start", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new StartLiveSessionCommand(id), ct)).ToHttpResult());

        teacher.MapPost("/{id:guid}/end", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new EndLiveSessionCommand(id), ct)).ToHttpResult());

        teacher.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CancelLiveSessionCommand(id), ct)).ToHttpResult());

        teacher.MapPost("/{id:guid}/reschedule", async (
            Guid id, RescheduleLiveSessionBody req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RescheduleLiveSessionCommand(
                id, req.NewScheduledAtUtc, req.NewDurationMinutes, req.TitleOverride), ct)).ToHttpResult());

        teacher.MapPut("/{id:guid}", async (Guid id, EditLiveSessionRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new EditLiveSessionCommand(
                id, req.Title, req.Description,
                req.SubjectId, req.StageId,
                req.ScheduledAtUtc, req.DurationMinutes), ct);
            return result.ToHttpResult();
        });

        teacher.MapPost("/{id:guid}/record/start", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new StartRecordingCommand(id), ct);
            return result.IsSuccess
                ? Results.Ok(new { egressId = result.Value, recording = true })
                : result.ToHttpResult();
        });

        teacher.MapPost("/{id:guid}/record/stop", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new StopRecordingCommand(id), ct)).ToHttpResult());

        // Client-side recording upload — used when Egress isn't available (LiveKit Cloud,
        // local dev, etc.). The teacher's browser records via MediaRecorder, then uploads
        // the resulting blob here. Saved to local recordings/ folder.
        teacher.MapPost("/{id:guid}/record/upload",
            async (Guid id, HttpContext ctx, IApplicationDbContext db,
                   ICurrentUser currentUser, IConfiguration config,
                   IWebHostEnvironment env, CancellationToken ct) =>
            {
                if (currentUser.UserId is null) return Results.Unauthorized();
                var session = await db.LiveSessions.FirstOrDefaultAsync(s => s.Id == id, ct);
                if (session is null) return Results.NotFound();
                if (session.TeacherId != currentUser.UserId) return Results.Forbid();
                if (!ctx.Request.HasFormContentType) return Results.BadRequest(new { message = "Expected multipart/form-data" });

                var form = await ctx.Request.ReadFormAsync(ct);
                var file = form.Files["file"];
                if (file is null || file.Length == 0)
                    return Results.BadRequest(new { message = "Missing 'file' part" });

                var recordingsPath = config["LiveKit:RecordingsPath"]
                    ?? Path.Combine(env.ContentRootPath, "recordings");
                if (!Directory.Exists(recordingsPath)) Directory.CreateDirectory(recordingsPath);

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(ext)) ext = ".webm";
                var fileName = $"{session.Id:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}";
                var fullPath = Path.Combine(recordingsPath, fileName);

                await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(fs, ct);
                }

                var url = $"/recordings/{fileName}";
                session.AttachRecording(url);
                await db.SaveChangesAsync(ct);

                return Results.Ok(new { url, sizeBytes = file.Length, fileName });
            })
           .DisableAntiforgery();

        // ─── Student ───────────────────────────────
        var student = app.MapGroup("/api/v1/student/live")
            .WithTags("Student.Live")
            .RequireAuthorization();

        student.MapGet("/upcoming", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetUpcomingLiveSessionsQuery(), ct)).ToHttpResult());

        student.MapGet("/recorded", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetRecordedLiveSessionsQuery(), ct)).ToHttpResult());

        // ─── Shared (join — generates LiveKit token) ──
        app.MapPost("/api/v1/live/{id:guid}/join",
            async (Guid id, ISender sender, ILiveKitTokenService tokens,
                   IOptions<LiveKitOptions> liveKitOpts, CancellationToken ct) =>
            {
                var result = await sender.Send(new JoinLiveSessionCommand(id), ct);
                if (result.IsFailure) return result.ToHttpResult();

                var data = result.Value;
                var lk = liveKitOpts.Value;

                // If LiveKit isn't configured (dev), return response without video grants.
                // The Blazor LiveRoom skips LiveKit connect when token/url is empty and falls
                // back to chat + placeholder video.
                if (string.IsNullOrEmpty(lk.ApiKey) || string.IsNullOrEmpty(lk.ApiSecret) || string.IsNullOrEmpty(lk.Url))
                    return Results.Ok(data);

                var token = tokens.GenerateToken(
                    roomName: data.RoomId,
                    userIdentity: data.SessionId.ToString() + ":" + (data.IsTeacher ? "teacher" : "student") + ":" + Guid.NewGuid(),
                    userName: data.CurrentUserName,
                    canPublish: data.IsTeacher,
                    validFor: TimeSpan.FromHours(4));

                var enriched = data with { LiveKitUrl = lk.Url, LiveKitToken = token };
                return Results.Ok(enriched);
            })
           .RequireAuthorization()
           .WithTags("Live");

        // ─── LiveKit Webhook (recording lifecycle) ────
        app.MapPost("/api/v1/webhooks/livekit",
            async (HttpContext ctx,
                   IApplicationDbContext db,
                   FSEdu.Application.Abstractions.INotificationService notifications,
                   IOptions<LiveKitOptions> opts,
                   ILoggerFactory loggerFactory,
                   CancellationToken ct) =>
            {
                var log = loggerFactory.CreateLogger("LiveKitWebhook");
                using var reader = new StreamReader(ctx.Request.Body);
                var body = await reader.ReadToEndAsync(ct);

                // Reject unsigned/invalid requests (defense against spoofed recording URLs).
                var auth = ctx.Request.Headers.Authorization.ToString();
                if (!FSEdu.Api.LiveKit.LiveKitWebhookVerifier.Verify(
                        auth, body, opts.Value.ApiKey, opts.Value.ApiSecret))
                {
                    log.LogWarning("LiveKit webhook rejected — bad signature");
                    return Results.Unauthorized();
                }

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    var evt = root.TryGetProperty("event", out var evtProp) ? evtProp.GetString() : null;
                    if (evt is null || !root.TryGetProperty("egressInfo", out var egress))
                        return Results.Ok();

                    var roomName = egress.TryGetProperty("roomName", out var rn) ? rn.GetString() : null;
                    if (string.IsNullOrEmpty(roomName)) return Results.Ok();

                    var session = db.LiveSessions
                        .Where(s => s.RoomId == roomName)
                        .OrderByDescending(s => s.ScheduledAtUtc)
                        .FirstOrDefault();
                    if (session is null) return Results.Ok();

                    if (evt == "egress_ended" &&
                        egress.TryGetProperty("fileResults", out var files) &&
                        files.GetArrayLength() > 0)
                    {
                        var filename = files[0].GetProperty("filename").GetString();
                        if (filename is not null)
                        {
                            var url = $"/recordings/{Path.GetFileName(filename)}";
                            session.AttachRecording(url);
                            await db.SaveChangesAsync(ct);

                            // Notify enrolled students that the recording is ready
                            if (session.SubjectId is int subjectId)
                            {
                                var now = DateTime.UtcNow;
                                var enrolled = await db.Subscriptions
                                    .Where(s => s.Status == FSEdu.Domain.Common.SubscriptionStatus.Active
                                             && s.StartsAtUtc <= now && s.EndsAtUtc >= now
                                             && ((s.Type != FSEdu.Domain.Common.SubscriptionType.StageFullTerm && s.SubjectId == subjectId)
                                                 || (s.Type == FSEdu.Domain.Common.SubscriptionType.StageFullTerm && s.StageId == session.StageId)))
                                    .Select(s => s.UserId)
                                    .Distinct()
                                    .ToListAsync(ct);

                                foreach (var uid in enrolled)
                                {
                                    await notifications.SendAsync(uid,
                                        "live.recording_ready",
                                        "📼 تسجيل الحصة جاهز!",
                                        $"تسجيل \"{session.Title}\" متاح للمشاهدة الآن.",
                                        new { sessionId = session.Id, url = "/student/recordings" }, ct);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "LiveKit webhook handler failed");
                }

                return Results.Ok();
            })
           .AllowAnonymous()
           .WithTags("Webhooks");

        return app;
    }
}

public sealed record RescheduleLiveSessionBody(DateTime NewScheduledAtUtc, int? NewDurationMinutes, string? TitleOverride);

public sealed record BulkScheduleBody(
    int SubjectId, int StageId,
    string TitlePattern, string? Description,
    DateTime FirstStartUtc, int DurationMinutes, int Occurrences);
