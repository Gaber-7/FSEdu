using FSEdu.Application.Features.Dashboard;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dashboard").WithTags("Dashboard").RequireAuthorization();

        group.MapGet("/student", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetStudentDashboardQuery(), ct)).ToHttpResult());

        group.MapGet("/teacher", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetTeacherDashboardQuery(), ct)).ToHttpResult());

        group.MapGet("/parent", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetParentDashboardQuery(), ct)).ToHttpResult());

        group.MapGet("/parent/child/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetChildDetailQuery(id), ct)).ToHttpResult());

        return app;
    }
}
