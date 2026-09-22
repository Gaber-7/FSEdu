using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Students;

// Aggregates the current student's recent activity from multiple tables
// (lessons, badges, assessments, questions, reviews, markers, subscriptions)
// and merges them into a single chronological feed.
public sealed record GetMyActivityFeedQuery(int Take = 30) : IQuery<List<ActivityItemDto>>;

public sealed class GetMyActivityFeedHandler : IQueryHandler<GetMyActivityFeedQuery, List<ActivityItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyActivityFeedHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<ActivityItemDto>>> Handle(GetMyActivityFeedQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var take = Math.Clamp(request.Take, 1, 100);
        // Pull twice the take from each source to give us enough headroom after merge+sort.
        var perSource = take;

        // ─── Lessons watched ────────────────────────────
        var lessons = await (
            from lp in _db.LessonProgress
            where lp.StudentId == userId
            join l in _db.Lessons on lp.LessonId equals l.Id
            orderby lp.LastViewedAtUtc descending
            select new ActivityItemDto(
                "lesson",
                lp.Completed ? "✅" : "▶",
                lp.Completed ? "أكملت درسًا" : "تابعت درسًا",
                $"{l.Title} · {l.Chapter.Course.Title}",
                lp.LastViewedAtUtc,
                $"/lessons/{l.Id}/watch"
            )).Take(perSource).ToListAsync(ct);

        // ─── Badges earned ──────────────────────────────
        var badges = await (
            from sb in _db.StudentBadges
            where sb.StudentId == userId
            join b in _db.Badges on sb.BadgeId equals b.Id
            orderby sb.AwardedAtUtc descending
            select new ActivityItemDto(
                "badge",
                "🏆",
                "حصلت على شارة",
                b.NameAr,
                sb.AwardedAtUtc,
                "/student/achievements"
            )).Take(perSource).ToListAsync(ct);

        // ─── Assessment attempts ────────────────────────
        var attempts = await (
            from a in _db.AssessmentAttempts
            where a.StudentId == userId && a.SubmittedAtUtc != null
            join asm in _db.Assessments on a.AssessmentId equals asm.Id
            orderby a.SubmittedAtUtc descending
            select new ActivityItemDto(
                "assessment",
                "📝",
                "قدّمت اختبارًا",
                $"{asm.Title} · الدرجة: {a.Score}",
                a.SubmittedAtUtc!.Value,
                $"/attempts/{a.Id}"
            )).Take(perSource).ToListAsync(ct);

        // ─── Questions asked on lessons ─────────────────
        var questions = await (
            from q in _db.LessonQuestions
            where q.AskedByUserId == userId
            join l in _db.Lessons on q.LessonId equals l.Id
            orderby q.CreatedAtUtc descending
            select new ActivityItemDto(
                "question",
                "❓",
                "سألت سؤالًا",
                $"على درس: {l.Title}",
                q.CreatedAtUtc,
                $"/lessons/{l.Id}/watch"
            )).Take(perSource).ToListAsync(ct);

        // ─── Course reviews ─────────────────────────────
        var reviews = await (
            from r in _db.CourseReviews
            where r.StudentId == userId
            join c in _db.Courses on r.CourseId equals c.Id
            orderby (r.UpdatedAtUtc ?? r.CreatedAtUtc) descending
            select new ActivityItemDto(
                "review",
                "⭐",
                "كتبت تقييمًا",
                $"{c.Title} · {r.Rating}/5",
                r.UpdatedAtUtc ?? r.CreatedAtUtc,
                $"/courses/{c.Id}"
            )).Take(perSource).ToListAsync(ct);

        // ─── Subscriptions ──────────────────────────────
        var subs = await (
            from s in _db.Subscriptions
            where s.UserId == userId
            orderby s.CreatedAtUtc descending
            select new ActivityItemDto(
                "subscription",
                "💳",
                "أنشأت اشتراكًا",
                s.Type.ToString(),
                s.CreatedAtUtc,
                "/student/subscriptions"
            )).Take(perSource).ToListAsync(ct);

        var all = lessons
            .Concat(badges)
            .Concat(attempts)
            .Concat(questions)
            .Concat(reviews)
            .Concat(subs)
            .OrderByDescending(i => i.AtUtc)
            .Take(take)
            .ToList();

        return Result.Success(all);
    }
}
