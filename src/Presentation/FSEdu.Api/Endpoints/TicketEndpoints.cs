using FSEdu.Application.Features.Tickets;
using FSEdu.Shared.Contracts.Tickets;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Student ─────────────────────────
        var student = app.MapGroup("/api/v1/student/tickets")
            .WithTags("Student.Tickets")
            .RequireAuthorization();

        student.MapGet("/my", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyTicketsQuery(), ct)).ToHttpResult());

        student.MapPost("/", async (CreateTicketRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateTicketCommand(
                req.SubjectId, req.Title, req.Body, req.ImageUrl, req.Priority), ct);
            return result.ToHttpResult();
        });

        // ─── Staff (Teacher / Assistant / Admin) ─────────
        var staff = app.MapGroup("/api/v1/staff/tickets")
            .WithTags("Staff.Tickets")
            .RequireAuthorization();

        staff.MapGet("/inbox", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAssignedTicketsQuery(), ct)).ToHttpResult());

        // ─── Shared (any participant or admin) ───────────
        var shared = app.MapGroup("/api/v1/tickets")
            .WithTags("Tickets")
            .RequireAuthorization();

        shared.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetTicketDetailsQuery(id), ct)).ToHttpResult());

        shared.MapPost("/{id:guid}/reply", async (
            Guid id, ReplyToTicketRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReplyToTicketCommand(
                id, req.Body, req.ImageUrl, req.AudioUrl), ct);
            return result.ToHttpResult();
        });

        shared.MapPost("/{id:guid}/resolve", async (
            Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ResolveTicketCommand(id), ct)).ToHttpResult());

        return app;
    }
}
