using FSEdu.Application.Features.Search;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/search",
            async (string q, int? per, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UniversalSearchQuery(q, per ?? 10), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Search");

        return app;
    }
}
