using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Teachers;

// Aggregates recent activity across all the teacher's courses into a
// single chronological feed: new enrollments, unanswered questions,
// new reviews (low-star prioritized), recent attempts (low-score
// prioritized), discussions, lesson reactions.
public sealed record GetTeacherActivityFeedQuery(int Take = 30) : IQuery<List<TeacherActivityItemDto>>;

public sealed class GetTeacherActivityFeedHandler
    : IQueryHandler<GetTeacherActivityFeedQuery, List<TeacherActivityItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetTeacherActivityFeedHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<TeacherActivityItemDto>>> Handle(
        GetTeacherActivityFeedQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var teacherId = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");

        var courseIds = await _db.Courses
            .Where(c => isAdmin || c.TeacherId == teacherId)
            .Select(c => c.Id)
            .ToListAsync(ct);
        if (courseIds.Count == 0)
            return Result.Success(new List<TeacherActivityItemDto>());

        var since = DateTime.UtcNow.AddDays(-30);
        var perSource = Math.Max(10, request.Take);

        // ─── Enrollments ─────────────────────────
        var enrollments = await (
            from e in _db.Enrollments
            where courseIds.Contains(e.CourseId) && e.EnrolledAtUtc >= since
            join s in _db.Students on e.StudentId equals s.Id
            join c in _db.Courses on e.CourseId equals c.Id
            orderby e.EnrolledAtUtc descending
            select new TeacherActivityItemDto(
                "enrollment", "👋",
                "طالب جديد التحق",
                $"{s.FullName} انضم لـ \"{c.Title}\"",
                c.Id, c.Title,
                s.FullName,
                e.EnrolledAtUtc,
                $"/teacher/courses/{c.Id}/roster",
                30
            )).Take(perSource).ToListAsync(ct);

        // ─── Questions (unanswered get higher priority) ──
        var questions = await (
            from q in _db.LessonQuestions
            join l in _db.Lessons on q.LessonId equals l.Id
            join ch in _db.Chapters on l.ChapterId equals ch.Id
            where courseIds.Contains(ch.CourseId) && q.CreatedAtUtc >= since
            orderby q.CreatedAtUtc descending
            select new TeacherActivityItemDto(
                "question", q.AnswerBody == null ? "❓" : "💬",
                q.AnswerBody == null ? "سؤال بانتظار الرد" : "سؤال جديد",
                $"{q.AskedByName}: {(q.Body.Length > 80 ? q.Body.Substring(0, 80) + "…" : q.Body)}",
                ch.CourseId, ch.Course.Title,
                q.AskedByName,
                q.CreatedAtUtc,
                "/teacher/qa-inbox",
                q.AnswerBody == null ? 90 : 40
            )).Take(perSource).ToListAsync(ct);

        // ─── Reviews (low ratings prioritized) ────────
        var reviews = await (
            from r in _db.CourseReviews
            where courseIds.Contains(r.CourseId) && r.CreatedAtUtc >= since
            join s in _db.Students on r.StudentId equals s.Id
            join c in _db.Courses on r.CourseId equals c.Id
            orderby r.CreatedAtUtc descending
            select new TeacherActivityItemDto(
                "review",
                r.Rating <= 2 ? "⚠️" : (r.Rating == 5 ? "🌟" : "⭐"),
                r.Rating <= 2 ? "تقييم منخفض!" : "تقييم جديد",
                $"{s.FullName} ({r.Rating}/5){(r.Comment != null && r.Comment.Length > 0 ? ": " + (r.Comment.Length > 60 ? r.Comment.Substring(0, 60) + "…" : r.Comment) : "")}",
                c.Id, c.Title,
                s.FullName,
                r.CreatedAtUtc,
                $"/courses/{c.Id}",
                r.Rating <= 2 ? 95 : (r.Rating == 5 ? 50 : 35)
            )).Take(perSource).ToListAsync(ct);

        // ─── Assessment attempts (low scores prioritized) ───
        var attempts = await (
            from a in _db.AssessmentAttempts
            join asm in _db.Assessments on a.AssessmentId equals asm.Id
            where asm.CourseId != null && courseIds.Contains(asm.CourseId.Value)
                  && a.SubmittedAtUtc != null && a.SubmittedAtUtc >= since
            join s in _db.Students on a.StudentId equals s.Id
            join c in _db.Courses on asm.CourseId equals c.Id
            orderby a.SubmittedAtUtc descending
            select new
            {
                AttemptId = a.Id, a.Score, asm.TotalMarks,
                AssessmentTitle = asm.Title,
                CourseId = c.Id, CourseTitle = c.Title,
                StudentName = s.FullName, SubmittedAtUtc = a.SubmittedAtUtc!.Value
            }
        ).Take(perSource).ToListAsync(ct);

        var attemptItems = attempts.Select(x =>
        {
            var pct = x.TotalMarks > 0 ? (int)Math.Round((double)(x.Score / x.TotalMarks * 100)) : 0;
            return new TeacherActivityItemDto(
                "attempt",
                pct < 40 ? "📉" : (pct >= 90 ? "🏆" : "📝"),
                pct < 40 ? "أداء منخفض في اختبار" : "اختبار تم تقديمه",
                $"{x.StudentName}: {x.AssessmentTitle} ({pct}%)",
                x.CourseId, x.CourseTitle,
                x.StudentName,
                x.SubmittedAtUtc,
                "/teacher/qa-inbox",
                pct < 40 ? 75 : (pct >= 90 ? 30 : 25)
            );
        });

        // ─── Discussions ─────────────────────────
        var discussions = await (
            from d in _db.CourseDiscussions
            where courseIds.Contains(d.CourseId) && d.CreatedAtUtc >= since
            join c in _db.Courses on d.CourseId equals c.Id
            orderby d.LastActivityAtUtc descending
            select new TeacherActivityItemDto(
                "discussion", "💬",
                d.RepliesCount > 0 ? "نقاش نشط" : "نقاش جديد",
                $"{d.AuthorName}: {d.Title}",
                c.Id, c.Title,
                d.AuthorName,
                d.LastActivityAtUtc,
                $"/courses/discussions/{d.Id}",
                d.RepliesCount == 0 ? 35 : 25
            )).Take(perSource).ToListAsync(ct);

        // ─── Lesson reactions (confusing ones get attention) ───
        var confusing = await (
            from r in _db.LessonReactions
            where r.Type == Domain.Courses.ReactionType.Confusing && r.CreatedAtUtc >= since
            join l in _db.Lessons on r.LessonId equals l.Id
            join ch in _db.Chapters on l.ChapterId equals ch.Id
            where courseIds.Contains(ch.CourseId)
            join c in _db.Courses on ch.CourseId equals c.Id
            join s in _db.Students on r.StudentId equals s.Id
            orderby r.CreatedAtUtc descending
            select new TeacherActivityItemDto(
                "reaction", "🤔",
                "طالب طلب توضيحًا",
                $"{s.FullName} على درس \"{l.Title}\"",
                c.Id, c.Title,
                s.FullName,
                r.CreatedAtUtc,
                $"/teacher/courses/{c.Id}/insights",
                60
            )).Take(perSource).ToListAsync(ct);

        var take = Math.Clamp(request.Take, 1, 100);
        var all = enrollments
            .Concat(questions)
            .Concat(reviews)
            .Concat(attemptItems)
            .Concat(discussions)
            .Concat(confusing)
            // Priority breaks ties when items happened within minutes of each other.
            .OrderByDescending(i => i.AtUtc)
            .ThenByDescending(i => i.Priority)
            .Take(take)
            .ToList();

        return Result.Success(all);
    }
}
