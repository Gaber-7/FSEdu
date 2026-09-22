using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Reference;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Api.Endpoints;

public static class ReferenceEndpoints
{
    public static IEndpointRouteBuilder MapReferenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reference").WithTags("Reference");

        group.MapGet("/stages", async (IApplicationDbContext db, CancellationToken ct) =>
        {
            var stages = await db.Stages
                .OrderBy(s => s.LevelOrder)
                .Select(s => new StageDto(s.Id, s.NameAr, s.NameEn, s.LevelOrder, s.ParentStageId))
                .ToListAsync(ct);
            return Results.Ok(stages);
        })
        .WithName("GetStages");

        group.MapGet("/subjects", async (int? stageId, IApplicationDbContext db, CancellationToken ct) =>
        {
            var query = db.Subjects.Where(s => s.IsActive);
            if (stageId.HasValue) query = query.Where(s => s.StageId == stageId.Value);

            var subjects = await query
                .OrderBy(s => s.NameAr)
                .Select(s => new SubjectDto(s.Id, s.StageId, s.NameAr, s.NameEn, s.IconUrl, s.ColorHex))
                .ToListAsync(ct);
            return Results.Ok(subjects);
        })
        .WithName("GetSubjects");

        group.MapGet("/regions", async (IApplicationDbContext db, CancellationToken ct) =>
        {
            var regions = await db.Regions
                .Where(r => r.IsActive)
                .OrderBy(r => r.NameAr)
                .Select(r => new RegionDto(r.Id, r.NameAr, r.NameEn, r.Code))
                .ToListAsync(ct);
            return Results.Ok(regions);
        })
        .WithName("GetRegions");

        group.MapGet("/schools", async (int? regionId, IApplicationDbContext db, CancellationToken ct) =>
        {
            var query = db.Schools.Where(s => s.IsActive);
            if (regionId.HasValue) query = query.Where(s => s.RegionId == regionId.Value);

            var schools = await query
                .OrderBy(s => s.Name)
                .Select(s => new SchoolDto(s.Id, s.Name, s.RegionId, s.Type.ToString()))
                .ToListAsync(ct);
            return Results.Ok(schools);
        })
        .WithName("GetSchools");

        return app;
    }
}
