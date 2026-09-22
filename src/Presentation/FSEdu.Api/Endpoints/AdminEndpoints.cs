using FSEdu.Application.Features.Admin;
using FSEdu.Application.Features.Admin.Teachers;
using FSEdu.Shared.Contracts.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace FSEdu.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/overview", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAdminOverviewQuery(), ct)).ToHttpResult());

        group.MapGet("/teachers/pending", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPendingTeachersQuery(), ct);
            return result.ToHttpResult();
        });

        group.MapPost("/teachers/{id:guid}/approve", async (
            Guid id, ApproveTeacherRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveTeacherCommand(id, req.Notes), ct);
            return result.ToHttpResult();
        });

        group.MapPost("/teachers/{id:guid}/reject", async (
            Guid id, RejectTeacherRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RejectTeacherCommand(id, req.Reason), ct);
            return result.ToHttpResult();
        });

        group.MapPost("/notifications/broadcast", async (
            BroadcastNotificationRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new BroadcastNotificationCommand(
                req.Target, req.Title, req.Body, req.Url), ct);
            return result.ToHttpResult();
        });

        // ─── Coupons ───────────────────────────────────
        group.MapGet("/coupons", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetCouponsListQuery(), ct)).ToHttpResult());

        group.MapPost("/coupons", async (CreateCouponRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateCouponCommand(
                req.Code, req.DiscountPct, req.DiscountFixed,
                req.ValidUntilUtc, req.MaxUses), ct)).ToHttpResult());

        group.MapPost("/coupons/{id:int}/deactivate", async (int id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeactivateCouponCommand(id), ct)).ToHttpResult());

        // ─── Sales Campaigns ──────────────────────────
        group.MapGet("/campaigns", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetCampaignsListQuery(), ct)).ToHttpResult());

        group.MapPost("/campaigns", async (CreateCampaignRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateCampaignCommand(
                req.Title, req.DiscountPct, req.Scope, req.ScopeId,
                req.ValidFromUtc, req.ValidUntilUtc), ct)).ToHttpResult());

        group.MapPost("/campaigns/{id:int}/set-active", async (int id, SetActiveBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetCampaignActiveCommand(id, body.Active), ct)).ToHttpResult());

        // ─── Discount Rules ─────────────────────────────
        group.MapGet("/discount-rules", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetDiscountRulesQuery(), ct)).ToHttpResult());

        group.MapPost("/discount-rules", async (CreateDiscountRuleRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateDiscountRuleCommand(
                req.Code, req.TitleAr, req.DescriptionAr,
                req.Type, req.DiscountPct, req.ThresholdInt,
                req.ValidFromUtc, req.ValidUntilUtc), ct)).ToHttpResult());

        group.MapPost("/discount-rules/{id:int}/set-active", async (int id, SetActiveBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetDiscountRuleActiveCommand(id, body.Active), ct)).ToHttpResult());

        // ─── Audit Log ──────────────────────────────────
        group.MapGet("/audit-log", async (
            string? action, Guid? actor, DateTime? from, DateTime? to,
            int? page, int? pageSize,
            ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAuditLogQuery(
                action, actor, from, to, page ?? 1, pageSize ?? 50), ct)).ToHttpResult());

        return app;
    }
}

public sealed record SetActiveBody(bool Active);
