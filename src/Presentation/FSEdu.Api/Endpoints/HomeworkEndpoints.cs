using FSEdu.Application.Features.Homework;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class HomeworkEndpoints
{
    public static void MapHomeworkEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Teacher group ────────────────────────────
        var t = app.MapGroup("/api/v1/teacher/homework")
                   .RequireAuthorization()
                   .WithTags("Teacher.Homework");

        t.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListMyTeacherHomeworkQuery(), ct)).ToHttpResult());

        t.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetTeacherHomeworkDetailQuery(id), ct)).ToHttpResult());

        t.MapPost("/", async (CreateHomeworkRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateHomeworkCommand(
                req.CourseId, req.Title, req.Description, req.AttachmentUrl,
                req.DueDateUtc, req.MaxScore, req.PublishNow), ct)).ToHttpResult());

        t.MapPut("/{id:guid}", async (Guid id, UpdateHomeworkRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateHomeworkCommand(
                id, req.Title, req.Description, req.AttachmentUrl,
                req.DueDateUtc, req.MaxScore), ct)).ToHttpResult());

        t.MapPost("/{id:guid}/publish", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ToggleHomeworkPublishCommand(id, true), ct)).ToHttpResult());

        t.MapPost("/{id:guid}/unpublish", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ToggleHomeworkPublishCommand(id, false), ct)).ToHttpResult());

        t.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteHomeworkCommand(id), ct)).ToHttpResult());

        t.MapPost("/submissions/{submissionId:guid}/grade",
            async (Guid submissionId, GradeHomeworkSubmissionRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GradeHomeworkSubmissionCommand(submissionId, req.Score, req.Feedback), ct)).ToHttpResult());

        // ─── Student group ────────────────────────────
        var s = app.MapGroup("/api/v1/student/homework")
                   .RequireAuthorization()
                   .WithTags("Student.Homework");

        s.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListMyStudentHomeworkQuery(), ct)).ToHttpResult());

        s.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetStudentHomeworkDetailQuery(id), ct)).ToHttpResult());

        s.MapPost("/{id:guid}/submit", async (Guid id, SubmitHomeworkRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SubmitHomeworkCommand(id, req.Body, req.AttachmentUrl), ct)).ToHttpResult());
    }
}
