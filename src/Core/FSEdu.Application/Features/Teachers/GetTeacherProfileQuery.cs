using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Contracts.Teachers;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Teachers;

public sealed record GetTeacherProfileQuery(Guid TeacherId) : IQuery<TeacherProfileDto>;

public sealed class GetTeacherProfileHandler : IQueryHandler<GetTeacherProfileQuery, TeacherProfileDto>
{
    private readonly IApplicationDbContext _db;

    public GetTeacherProfileHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherProfileDto>> Handle(GetTeacherProfileQuery request, CancellationToken ct)
    {
        var teacher = await _db.Teachers
            .Include(t => t.Subjects).ThenInclude(s => s.Subject)
            .Include(t => t.Regions).ThenInclude(r => r.Region)
            .Include(t => t.Qualifications)
            .FirstOrDefaultAsync(t => t.Id == request.TeacherId, ct);
        if (teacher is null)
            return Error.NotFound("TEACHER.NOT_FOUND", "المدرّس غير موجود", "Not found");

        var courses = await _db.Courses
            .Where(c => c.TeacherId == request.TeacherId && c.Status == CourseStatus.Published)
            .Include(c => c.Subject)
            .Include(c => c.Stage)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .OrderByDescending(c => c.RatingAvg)
            .ThenByDescending(c => c.EnrollmentCount)
            .ToListAsync(ct);

        var totalLessons = courses.Sum(c => c.Chapters.Sum(ch => ch.Lessons.Count));
        var totalEnrollments = courses.Sum(c => c.EnrollmentCount);

        var recordingsCount = await _db.LiveSessions
            .CountAsync(s => s.TeacherId == request.TeacherId
                          && s.Status == LiveSessionStatus.Ended
                          && s.RecordingUrl != null, ct);
        var liveSessionsHosted = await _db.LiveSessions
            .CountAsync(s => s.TeacherId == request.TeacherId
                          && (s.Status == LiveSessionStatus.Ended || s.Status == LiveSessionStatus.Live), ct);

        var publishedCourses = courses.Select(c => new CourseListItemDto(
            c.Id, c.Title, c.ThumbnailUrl,
            c.SubjectId, c.Subject.NameAr,
            c.StageId, c.Stage.NameAr,
            c.Status.ToString(),
            c.EnrollmentCount, c.RatingAvg,
            c.Chapters.Sum(ch => ch.Lessons.Count)
        )).ToArray();

        var dto = new TeacherProfileDto(
            teacher.Id,
            teacher.FullName,
            teacher.Bio,
            teacher.YearsOfExperience,
            teacher.RatingAvg,
            teacher.RatingsCount,
            teacher.Verified,
            teacher.Subjects.Select(s => s.Subject.NameAr).ToArray(),
            teacher.Regions.Select(r => r.Region.NameAr).ToArray(),
            teacher.Qualifications
                .OrderByDescending(q => q.Year)
                .Select(q => new TeacherQualificationDto(q.Id, q.Title, q.Institution, q.Year))
                .ToArray(),
            new TeacherProfileStatsDto(
                courses.Count, totalLessons, totalEnrollments,
                recordingsCount, liveSessionsHosted),
            publishedCourses,
            teacher.AvatarUrl,
            teacher.TotpEnabled);

        return Result.Success(dto);
    }
}
