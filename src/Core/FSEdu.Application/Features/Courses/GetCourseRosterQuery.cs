using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// Class roster — the teacher's view of every student enrolled in a course,
// with per-student progress and last activity. Used to know who needs
// follow-up, who's behind, and who has stopped engaging.
public sealed record GetCourseRosterQuery(Guid CourseId, int Take = 200)
    : IQuery<CourseRosterDto>;

public sealed class GetCourseRosterHandler : IQueryHandler<GetCourseRosterQuery, CourseRosterDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCourseRosterHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<CourseRosterDto>> Handle(GetCourseRosterQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var course = await _db.Courses
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        var isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin && course.TeacherId != userId)
            return Error.Forbidden("COURSE.NOT_YOURS", "هذه الدورة ليست لك", "Not yours");

        var lessonIds = course.Chapters.SelectMany(ch => ch.Lessons).Select(l => l.Id).ToList();
        var totalLessons = lessonIds.Count;

        var take = Math.Clamp(request.Take, 1, 500);

        // 1) Enrollments + Student info (single query)
        var enrollments = await (
            from e in _db.Enrollments
            where e.CourseId == request.CourseId
            join s in _db.Students on e.StudentId equals s.Id
            join st in _db.Stages on s.StageId equals st.Id
            orderby e.EnrolledAtUtc descending
            select new
            {
                e.StudentId,
                s.FullName,
                Phone = s.Phone.Value,
                Email = s.Email == null ? null : s.Email.Value,
                StageName = st.NameAr,
                s.AvatarUrl,
                e.EnrolledAtUtc,
                e.ProgressPct
            })
            .Take(take)
            .ToListAsync(ct);

        if (enrollments.Count == 0)
        {
            return Result.Success(new CourseRosterDto(
                course.Id, course.Title, 0, totalLessons, Array.Empty<RosterStudentDto>()));
        }

        var studentIds = enrollments.Select(x => x.StudentId).ToList();

        // 2) Completion counts per student for this course's lessons
        var completedByStudent = await _db.LessonProgress
            .Where(lp => studentIds.Contains(lp.StudentId)
                      && lessonIds.Contains(lp.LessonId)
                      && lp.Completed)
            .GroupBy(lp => lp.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count, ct);

        // 3) Last viewed time per student in this course
        var lastViewedByStudent = await _db.LessonProgress
            .Where(lp => studentIds.Contains(lp.StudentId) && lessonIds.Contains(lp.LessonId))
            .GroupBy(lp => lp.StudentId)
            .Select(g => new { StudentId = g.Key, Last = g.Max(x => x.LastViewedAtUtc) })
            .ToDictionaryAsync(x => x.StudentId, x => x.Last, ct);

        // 4) Most recent assessment score for this course's assessments per student
        var courseAssessmentIds = await _db.Assessments
            .Where(a => a.CourseId == request.CourseId)
            .Select(a => new { a.Id, a.TotalMarks })
            .ToListAsync(ct);
        var totalsByAssessment = courseAssessmentIds.ToDictionary(x => x.Id, x => x.TotalMarks);
        var asmIds = courseAssessmentIds.Select(x => x.Id).ToList();

        var lastAttempt = await _db.AssessmentAttempts
            .Where(a => studentIds.Contains(a.StudentId)
                     && asmIds.Contains(a.AssessmentId)
                     && a.SubmittedAtUtc != null)
            .GroupBy(a => a.StudentId)
            .Select(g => g.OrderByDescending(x => x.SubmittedAtUtc)
                          .Select(x => new { x.StudentId, x.AssessmentId, x.Score })
                          .First())
            .ToListAsync(ct);
        var lastScoreByStudent = lastAttempt.ToDictionary(
            x => x.StudentId,
            x => totalsByAssessment.TryGetValue(x.AssessmentId, out var t) && t > 0
                ? Math.Round((decimal)x.Score / t * 100m, 1)
                : 0m);

        var rows = enrollments.Select(e => new RosterStudentDto(
            e.StudentId,
            e.FullName, e.Phone, e.Email, e.StageName, e.AvatarUrl,
            e.EnrolledAtUtc,
            completedByStudent.TryGetValue(e.StudentId, out var done) ? done : 0,
            Math.Round(e.ProgressPct, 1),
            lastViewedByStudent.TryGetValue(e.StudentId, out var last) ? last : (DateTime?)null,
            lastScoreByStudent.TryGetValue(e.StudentId, out var sc) ? sc : (decimal?)null
        )).ToArray();

        return Result.Success(new CourseRosterDto(
            course.Id, course.Title,
            course.EnrollmentCount, totalLessons, rows));
    }
}
