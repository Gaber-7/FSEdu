using FSEdu.Application.Features.Assessments.Queries;
using FSEdu.Application.Features.Assessments.Student;
using FSEdu.Application.Features.Assessments.Teacher;
using FSEdu.Shared.Contracts.Assessments;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class AssessmentEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Teacher ──────────────────────────────
        var teacher = app.MapGroup("/api/v1/teacher/assessments")
            .WithTags("Teacher.Assessments")
            .RequireAuthorization(p => p.RequireRole("Teacher"));

        teacher.MapPost("/", async (CreateAssessmentRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateAssessmentCommand(
                req.CourseId, req.Type, req.Title, req.Description,
                req.TimeLimitMinutes, req.PassingMarks, req.AttemptsAllowed,
                req.AvailableFromUtc, req.AvailableToUtc), ct);
            return result.ToHttpResult();
        });

        teacher.MapPost("/{id:guid}/questions", async (
            Guid id, AddQuestionRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddQuestionCommand(
                id, req.Type, req.Difficulty, req.QuestionText,
                req.Options, req.CorrectOptionIndex, req.CorrectBoolean,
                req.Explanation, req.Marks), ct);
            return result.ToHttpResult();
        });

        teacher.MapDelete("/{id:guid}/questions/{questionId:guid}", async (
            Guid id, Guid questionId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RemoveQuestionCommand(id, questionId), ct)).ToHttpResult());

        // ─── Public/Shared ────────────────────────
        app.MapGet("/api/v1/courses/{courseId:guid}/assessments",
            async (Guid courseId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetCourseAssessmentsQuery(courseId), ct)).ToHttpResult())
           .WithTags("Assessments");

        app.MapGet("/api/v1/assessments/{id:guid}",
            async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAssessmentDetailsQuery(id), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Assessments");

        // ─── Student ──────────────────────────────
        var student = app.MapGroup("/api/v1/student/assessments")
            .WithTags("Student.Assessments")
            .RequireAuthorization();

        student.MapPost("/{id:guid}/start", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new StartAttemptCommand(id), ct)).ToHttpResult());

        var attempts = app.MapGroup("/api/v1/student/attempts")
            .WithTags("Student.Attempts")
            .RequireAuthorization();

        attempts.MapPost("/{id:guid}/submit", async (
            Guid id, SubmitAttemptRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SubmitAttemptCommand(id, req.Answers), ct)).ToHttpResult());

        attempts.MapGet("/{id:guid}/result", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAttemptResultQuery(id), ct)).ToHttpResult());

        return app;
    }
}
