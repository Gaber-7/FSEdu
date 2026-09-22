using FSEdu.Application.Abstractions;
using FSEdu.Application.Features.ParentZone;
using FSEdu.Domain.Users;
using FSEdu.Shared.Contracts.Parent;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Api.Endpoints;

public static class ParentEndpoints
{
    public static IEndpointRouteBuilder MapParentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/parent")
            .WithTags("Parent")
            .RequireAuthorization(policy => policy.RequireRole("Parent"));

        group.MapGet("/children", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyChildrenQuery(), ct);
            return result.ToHttpResult();
        });

        group.MapPost("/children/link", async (LinkChildRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LinkChildCommand(req.StudentPhone, req.Relation), ct);
            return result.ToHttpResult();
        });

        // ─── Weekly digest ─────────────────────────
        group.MapPost("/digest/send-now",
            async (ICurrentUser cu, IWeeklyDigestService svc, CancellationToken ct) =>
            {
                if (cu.UserId is null) return Results.Unauthorized();
                var ok = await svc.SendForParentAsync(cu.UserId.Value, ct);
                return ok ? Results.Ok(new { sent = true }) : Results.BadRequest(new { sent = false });
            });

        group.MapPost("/digest/toggle",
            async (DigestTogglePayload body, ICurrentUser cu, IApplicationDbContext db, CancellationToken ct) =>
            {
                if (cu.UserId is null) return Results.Unauthorized();
                var parent = await db.Users.OfType<Parent>().FirstOrDefaultAsync(p => p.Id == cu.UserId.Value, ct);
                if (parent is null) return Results.NotFound();
                parent.SetWeeklyDigestEnabled(body.Enabled);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { enabled = body.Enabled });
            });

        group.MapGet("/digest/status",
            async (ICurrentUser cu, IApplicationDbContext db, CancellationToken ct) =>
            {
                if (cu.UserId is null) return Results.Unauthorized();
                var p = await db.Users.OfType<Parent>()
                    .Where(p => p.Id == cu.UserId.Value)
                    .Select(p => new { p.WeeklyDigestEnabled, p.LastWeeklyDigestAtUtc })
                    .FirstOrDefaultAsync(ct);
                if (p is null) return Results.NotFound();
                return Results.Ok(p);
            });

        return app;
    }
}

public sealed record DigestTogglePayload(bool Enabled);
