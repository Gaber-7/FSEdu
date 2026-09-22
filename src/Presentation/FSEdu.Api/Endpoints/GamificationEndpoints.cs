using FSEdu.Application.Abstractions;
using FSEdu.Application.Features.Courses;
using FSEdu.Application.Features.Gamification;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class GamificationEndpoints
{
    public static IEndpointRouteBuilder MapGamificationEndpoints(this IEndpointRouteBuilder app)
    {
        var pub = app.MapGroup("/api/v1").WithTags("Gamification");

        pub.MapGet("/leaderboard", async (int? stageId, int? take, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetLeaderboardQuery(stageId, take ?? 50), ct)).ToHttpResult());

        pub.MapGet("/student/achievements", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyAchievementsQuery(), ct)).ToHttpResult())
           .RequireAuthorization();

        // Daily challenge — student-only
        pub.MapGet("/student/daily-challenge",
            async (IDailyChallengeService svc, ICurrentUser cu, CancellationToken ct) =>
            {
                if (cu.UserId is null) return Results.Unauthorized();
                var dto = await svc.GetTodayStatusAsync(cu.UserId.Value, ct);
                return Results.Ok(dto);
            })
            .RequireAuthorization();

        // Lesson view tracking (Student only)
        pub.MapPost("/student/lessons/{id:guid}/track",
            async (Guid id, TrackLessonViewBody body, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new TrackLessonViewCommand(
                    id, body.LastPositionSec, body.WatchedPct), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization();

        return app;
    }
}

public sealed record TrackLessonViewBody(int LastPositionSec, decimal WatchedPct);
