using FSEdu.Application.Features.Subscriptions;
using FSEdu.Shared.Contracts.Subscriptions;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/subscriptions").WithTags("Subscriptions");

        // Public
        group.MapGet("/pricing", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPricingQuery(), ct)).ToHttpResult());

        // Authenticated
        group.MapPost("/subscribe", async (SubscribeRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SubscribeCommand(req.Type, req.SubjectId, req.StageId, req.Term), ct);
            return result.ToHttpResult();
        }).RequireAuthorization();

        group.MapGet("/my", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMySubscriptionsQuery(), ct)).ToHttpResult())
           .RequireAuthorization();

        return app;
    }
}
