using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Admin;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin.Teachers;

public sealed record GetPendingTeachersQuery() : IQuery<List<PendingTeacherDto>>;

public sealed class GetPendingTeachersHandler : IQueryHandler<GetPendingTeachersQuery, List<PendingTeacherDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPendingTeachersHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<PendingTeacherDto>>> Handle(GetPendingTeachersQuery request, CancellationToken ct)
    {
        var teachers = await _db.Teachers
            .Where(t => !t.Verified && t.Status == UserStatus.Pending)
            .Include(t => t.Subjects).ThenInclude(s => s.Subject)
            .Include(t => t.Regions).ThenInclude(r => r.Region)
            .Include(t => t.Qualifications)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(ct);

        var result = teachers.Select(t => new PendingTeacherDto(
            t.Id,
            t.FullName,
            t.Phone.Value,
            t.Email?.Value,
            t.Bio,
            t.YearsOfExperience,
            t.Subjects.Select(s => s.Subject.NameAr).ToArray(),
            t.Regions.Select(r => r.Region.NameAr).ToArray(),
            t.Qualifications.Select(q => new QualificationSummary(q.Title, q.Institution, q.Year, q.DocumentUrl)).ToArray(),
            t.CreatedAtUtc
        )).ToList();

        return Result.Success(result);
    }
}
