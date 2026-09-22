using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Students;

// "AI-style" suggestions — a small bag of deterministic rules evaluated
// against the student's recent activity. Returned ordered by priority,
// truncated to top N. Cheap (no entity, no migration, no ML).
public sealed record GetMySuggestionsQuery(int Take = 4) : IQuery<List<SuggestionDto>>;

public sealed class GetMySuggestionsHandler : IQueryHandler<GetMySuggestionsQuery, List<SuggestionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMySuggestionsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<SuggestionDto>>> Handle(GetMySuggestionsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Success(new List<SuggestionDto>());

        var userId = _currentUser.UserId.Value;
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == userId, ct);
        if (student is null)
            return Result.Success(new List<SuggestionDto>());

        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var tomorrowStart = todayStart.AddDays(1);
        var suggestions = new List<SuggestionDto>();

        // ─── Rule 1: Streak in danger ─────────────────────────
        // Has an active streak but hasn't viewed a lesson today.
        if (student.CurrentStreakDays > 0)
        {
            var viewedToday = await _db.LessonProgress.AnyAsync(lp =>
                lp.StudentId == userId
                && lp.LastViewedAtUtc >= todayStart
                && lp.LastViewedAtUtc < tomorrowStart, ct);
            if (!viewedToday)
            {
                suggestions.Add(new SuggestionDto(
                    "streak_risk",
                    "🔥",
                    $"سلسلتك ({student.CurrentStreakDays} يوم) في خطر!",
                    "شاهد درسًا واحدًا اليوم للحفاظ على سلسلتك.",
                    "تصفّح الدورات",
                    "/courses",
                    Priority: 100
                ));
            }
        }

        // ─── Rule 2: Stale ─────────────────────────────────────
        // No activity in 3+ days — and the student has progress already.
        var lastView = await _db.LessonProgress
            .Where(lp => lp.StudentId == userId)
            .OrderByDescending(lp => lp.LastViewedAtUtc)
            .Select(lp => (DateTime?)lp.LastViewedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (lastView.HasValue && (now - lastView.Value).TotalDays >= 3)
        {
            suggestions.Add(new SuggestionDto(
                "stale",
                "📚",
                "اشتقنا لك!",
                $"لم تشاهد درسًا منذ {(int)(now - lastView.Value).TotalDays} أيام. ارجع وأكمل من حيث توقّفت.",
                "تابع المشاهدة",
                "/dashboard/student",
                Priority: 80
            ));
        }

        // ─── Rule 3: Upcoming live in <= 2 hours ──────────────
        var soon = now.AddHours(2);
        var upcoming = await (
            from sess in _db.LiveSessions
            where sess.Status == LiveSessionStatus.Scheduled
                  && sess.ScheduledAtUtc > now && sess.ScheduledAtUtc <= soon
            join sub in _db.Subjects on sess.SubjectId equals sub.Id into sj
            from sub in sj.DefaultIfEmpty()
            orderby sess.ScheduledAtUtc
            select new { sess.Id, sess.Title, sess.ScheduledAtUtc, SubjectName = sub != null ? sub.NameAr : null }
        ).FirstOrDefaultAsync(ct);
        if (upcoming is not null)
        {
            var minsLeft = (int)Math.Max(1, Math.Round((upcoming.ScheduledAtUtc - now).TotalMinutes));
            suggestions.Add(new SuggestionDto(
                "upcoming_live",
                "🔴",
                $"حصّة قادمة خلال {minsLeft} دقيقة",
                $"{upcoming.Title}{(upcoming.SubjectName is null ? "" : $" · {upcoming.SubjectName}")}",
                "اذهب للحصص",
                "/student/live",
                Priority: 95
            ));
        }

        // ─── Rule 4: Subscription expiring in <= 7 days ───────
        var subExpiringIn = (DateTime?)null;
        var soonestActiveSub = await _db.Subscriptions
            .Where(s => s.UserId == userId
                     && s.Status == SubscriptionStatus.Active
                     && s.EndsAtUtc > now)
            .OrderBy(s => s.EndsAtUtc)
            .Select(s => s.EndsAtUtc)
            .FirstOrDefaultAsync(ct);
        if (soonestActiveSub != default && (soonestActiveSub - now).TotalDays <= 7)
            subExpiringIn = soonestActiveSub;

        if (subExpiringIn.HasValue)
        {
            var daysLeft = (int)Math.Ceiling((subExpiringIn.Value - now).TotalDays);
            suggestions.Add(new SuggestionDto(
                "subscription_expiring",
                "⏳",
                $"اشتراكك ينتهي خلال {daysLeft} {(daysLeft == 1 ? "يوم" : "أيام")}",
                "جدّد اشتراكك للحفاظ على الوصول لكل الدروس والحصص.",
                "اذهب لاشتراكاتي",
                "/student/subscriptions",
                Priority: 90
            ));
        }

        // ─── Rule 5: Low recent assessment score ──────────────
        var recentAttempts = await _db.AssessmentAttempts
            .Where(a => a.StudentId == userId && a.SubmittedAtUtc != null
                     && a.SubmittedAtUtc >= now.AddDays(-14))
            .OrderByDescending(a => a.SubmittedAtUtc)
            .Take(3)
            .Select(a => new { a.Id, a.Score, a.AssessmentId })
            .ToListAsync(ct);

        if (recentAttempts.Count >= 2)
        {
            // If the last 2+ attempts averaged < 50% raw score (using max marks lookup)
            var assessmentIds = recentAttempts.Select(a => a.AssessmentId).Distinct().ToList();
            var totals = await _db.Assessments
                .Where(a => assessmentIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.TotalMarks, ct);

            decimal sumPct = 0; int counted = 0;
            foreach (var a in recentAttempts)
            {
                if (totals.TryGetValue(a.AssessmentId, out var total) && total > 0)
                {
                    sumPct += (a.Score / total) * 100m;
                    counted++;
                }
            }
            if (counted >= 2 && sumPct / counted < 50)
            {
                suggestions.Add(new SuggestionDto(
                    "low_score",
                    "📝",
                    "حسّن أداءك في الاختبارات",
                    $"متوسط آخر {counted} اختبارات تحت 50%. راجع الدروس قبل المحاولة التالية.",
                    "افتح تقدّمي",
                    "/student/progress",
                    Priority: 70
                ));
            }
        }

        // ─── Rule 6: First-time onboarding hints ──────────────
        var hasAnyNote = await _db.LessonNotes.AnyAsync(n => n.StudentId == userId, ct);
        var hasAnyProgress = await _db.LessonProgress.AnyAsync(lp => lp.StudentId == userId, ct);
        if (!hasAnyNote && hasAnyProgress)
        {
            suggestions.Add(new SuggestionDto(
                "first_notes",
                "💡",
                "جرّب كتابة ملاحظات أثناء الدروس",
                "الطلاب اللي بياخدوا ملاحظات بيحصلوا على نتائج أعلى بـ 20%. اضغط أسفل أي درس لتجربتها.",
                "تابع المشاهدة",
                "/dashboard/student",
                Priority: 40
            ));
        }

        var hasAnyAttempt = await _db.AssessmentAttempts.AnyAsync(a => a.StudentId == userId, ct);
        if (!hasAnyAttempt && hasAnyProgress)
        {
            suggestions.Add(new SuggestionDto(
                "first_assessment",
                "🎯",
                "اختبر معرفتك",
                "حلّ أول اختبار لتقييم فهمك واحصل على XP إضافي.",
                "افتح الدورات",
                "/courses",
                Priority: 45
            ));
        }

        var take = Math.Clamp(request.Take, 1, 10);
        var result = suggestions
            .OrderByDescending(s => s.Priority)
            .Take(take)
            .ToList();

        return Result.Success(result);
    }
}
