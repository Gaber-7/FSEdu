using FSEdu.Application.Features.Teachers;
using FSEdu.Shared.Contracts.Teachers;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class TeacherEndpoints
{
    public static IEndpointRouteBuilder MapTeacherEndpoints(this IEndpointRouteBuilder app)
    {
        // Public — student/parent can view a teacher's profile to decide on a course.
        app.MapGet("/api/v1/teachers/{id:guid}",
            async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetTeacherProfileQuery(id), ct)).ToHttpResult())
           .WithTags("Teachers");

        // Self-edit — current teacher updates their own profile.
        var selfGroup = app.MapGroup("/api/v1/teacher/profile")
            .WithTags("Teacher.Profile")
            .RequireAuthorization(p => p.RequireRole("Teacher"));

        selfGroup.MapPut("/",
            async (UpdateTeacherProfileRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateTeacherProfileCommand(req.Bio, req.YearsOfExperience), ct))
                    .ToHttpResult());

        selfGroup.MapPost("/qualifications",
            async (AddTeacherQualificationRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddTeacherQualificationCommand(req.Title, req.Institution, req.Year), ct))
                    .ToHttpResult());

        selfGroup.MapDelete("/qualifications/{qualId:long}",
            async (long qualId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteTeacherQualificationCommand(qualId), ct))
                    .ToHttpResult());

        return app;
    }
}
