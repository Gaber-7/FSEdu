using FSEdu.Application.Features.PastPapers;
using FSEdu.Shared.Contracts.Assessments;
using FSEdu.Shared.Kernel.Results;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class PastPaperEndpoints
{
    public static void MapPastPaperEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/past-papers")
                   .RequireAuthorization()
                   .WithTags("PastPapers");

        g.MapGet("/", async (int? subjectId, int? stageId, int? year, string? term, string? examType,
                             ISender sender, CancellationToken ct) =>
            (await sender.Send(new BrowsePastPapersQuery(subjectId, stageId, year, term, examType), ct)).ToHttpResult());

        g.MapGet("/filters", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPastPaperFiltersQuery(), ct)).ToHttpResult());

        g.MapPost("/{id:guid}/attempts", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new StartPastPaperAttemptCommand(id), ct)).ToHttpResult());

        g.MapPost("/attempts/submit", async (SubmitPastPaperAttemptRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SubmitPastPaperAttemptCommand(req.AttemptId, req.Answers.ToList()), ct)).ToHttpResult());

        g.MapGet("/attempts/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPastPaperAttemptResultQuery(id), ct)).ToHttpResult());
    }
}
