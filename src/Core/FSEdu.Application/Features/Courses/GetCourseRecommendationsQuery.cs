using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// Suggests courses to the current student based on their stage and what
// they haven't already enrolled in. Ranked by rating + popularity.
public sealed record GetCourseRecommendationsQuery(int Take = 6)
    : IQuery<List<CourseListItemDto>>;

public sealed class GetCourseRecommendationsHandler
    : IQueryHandler<GetCourseRecommendationsQuery, List<CourseListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCourseRecommendationsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<CourseListItemDto>>> Handle(
        GetCourseRecommendationsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Success(new List<CourseListItemDto>());

        var studentId = _currentUser.UserId.Value;
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
            return Result.Success(new List<CourseListItemDto>());

        // Already enrolled or bookmarked courses → exclude
        var enrolledIds = await _db.Enrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => e.CourseId)
            .ToListAsync(ct);
        var bookmarkedIds = await _db.CourseBookmarks
            .Where(b => b.StudentId == studentId)
            .Select(b => b.CourseId)
            .ToListAsync(ct);
        var excludeSet = enrolledIds.Concat(bookmarkedIds).Distinct().ToHashSet();

        // Subjects the student has shown interest in (notes / progress / questions)
        var notedSubjects = await _db.LessonNotes
            .Where(n => n.StudentId == studentId)
            .Select(n => n.Lesson.Chapter.Course.SubjectId)
            .Distinct()
            .ToListAsync(ct);
        var watchedSubjects = await (
            from lp in _db.LessonProgress
            where lp.StudentId == studentId
            join l in _db.Lessons on lp.LessonId equals l.Id
            select l.Chapter.Course.SubjectId
        ).Distinct().ToListAsync(ct);
        var interestedSubjects = notedSubjects.Concat(watchedSubjects).Distinct().ToHashSet();

        var pool = _db.Courses
            .Where(c => c.Status == CourseStatus.Published && c.StageId == student.StageId);
        if (excludeSet.Count > 0)
            pool = pool.Where(c => !excludeSet.Contains(c.Id));

        var take = Math.Clamp(request.Take, 1, 24);

        // Score courses: prefer subjects the student already engaged with, then rating, then enrollment
        var courses = await pool
            .Select(c => new
            {
                c.Id, c.Title, c.ThumbnailUrl,
                c.SubjectId, SubjectName = c.Subject.NameAr,
                c.StageId, StageName = c.Stage.NameAr,
                c.Status, c.EnrollmentCount, c.RatingAvg,
                LessonsCount = c.Chapters.Sum(ch => ch.Lessons.Count),
                IsInterested = interestedSubjects.Contains(c.SubjectId)
            })
            .OrderByDescending(c => c.IsInterested)
            .ThenByDescending(c => c.RatingAvg)
            .ThenByDescending(c => c.EnrollmentCount)
            .Take(take * 3)
            .ToListAsync(ct);

        var list = courses
            .Take(take)
            .Select(c => new CourseListItemDto(
                c.Id, c.Title, c.ThumbnailUrl,
                c.SubjectId, c.SubjectName,
                c.StageId, c.StageName,
                c.Status.ToString(),
                c.EnrollmentCount, c.RatingAvg,
                c.LessonsCount))
            .ToList();

        return Result.Success(list);
    }
}
