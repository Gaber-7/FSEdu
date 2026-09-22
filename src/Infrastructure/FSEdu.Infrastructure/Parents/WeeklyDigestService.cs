using System.Net;
using System.Text;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSEdu.Infrastructure.Parents;

// Single-shot digest sender used both by the scheduled hosted service and by
// the parent's "Send me a sample digest" manual button. Pure compute → HTML →
// IEmailSender; no transaction commit beyond stamping LastWeeklyDigestAtUtc.
public sealed class WeeklyDigestService : IWeeklyDigestService
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<WeeklyDigestService> _logger;

    public WeeklyDigestService(IApplicationDbContext db, IEmailSender email,
                                ILogger<WeeklyDigestService> logger)
    { _db = db; _email = email; _logger = logger; }

    public async Task<bool> SendForParentAsync(Guid parentId, CancellationToken ct = default)
    {
        var parent = await _db.Users.OfType<Parent>()
            .Include(p => p.Children).ThenInclude(c => c.Student)
            .FirstOrDefaultAsync(p => p.Id == parentId, ct);
        if (parent is null) return false;
        if (parent.Email is null || string.IsNullOrWhiteSpace(parent.Email.Value)) return false;
        if (parent.Children.Count == 0) return false;

        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var blocks = new List<string>();

        foreach (var link in parent.Children)
        {
            var child = link.Student;
            if (child is null) continue;

            var lessonsCompleted = await _db.LessonProgress
                .CountAsync(lp => lp.StudentId == child.Id && lp.Completed
                                  && lp.LastViewedAtUtc >= weekAgo, ct);

            // Watch minutes approximated from LessonProgress × Lesson duration × Pct
            var watchedRows = await (
                from lp in _db.LessonProgress
                where lp.StudentId == child.Id && lp.LastViewedAtUtc >= weekAgo
                join l in _db.Lessons on lp.LessonId equals l.Id
                select new { l.DurationSeconds, lp.WatchedPct }
            ).ToListAsync(ct);
            var watchMinutes = (int)Math.Round(
                watchedRows.Sum(r => r.DurationSeconds * (double)r.WatchedPct / 100.0) / 60.0);

            var assessmentsTaken = await _db.AssessmentAttempts
                .CountAsync(a => a.StudentId == child.Id && a.SubmittedAtUtc >= weekAgo, ct);

            var avgScore = await _db.AssessmentAttempts
                .Where(a => a.StudentId == child.Id && a.SubmittedAtUtc >= weekAgo)
                .Select(a => (double?)a.Score)
                .AverageAsync(ct) ?? 0;

            var homeworkSubmitted = await _db.HomeworkSubmissions
                .CountAsync(s => s.StudentId == child.Id && s.SubmittedAtUtc >= weekAgo, ct);

            var pastPapers = await _db.PastPaperAttempts
                .CountAsync(a => a.StudentId == child.Id && a.SubmittedAtUtc >= weekAgo, ct);

            blocks.Add(BuildChildBlock(child.FullName, lessonsCompleted, watchMinutes,
                assessmentsTaken, avgScore, homeworkSubmitted, pastPapers,
                child.CurrentStreakDays, child.StreakShields));
        }

        if (blocks.Count == 0) return false;

        var html = BuildEmail(parent.FullName, blocks);
        var subject = $"📊 تقرير أبنائك الأسبوعى — {DateTime.UtcNow:yyyy/MM/dd}";

        var sent = await _email.SendAsync(parent.Email.Value, subject, html, ct);
        if (sent)
        {
            parent.RecordWeeklyDigestSent();
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Weekly digest sent to parent {Id}", parent.Id);
        }
        return sent;
    }

    public async Task<int> SendDueDigestsAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var dueParentIds = await _db.Users.OfType<Parent>()
            .Where(p => p.WeeklyDigestEnabled
                    && p.Email != null
                    && (p.LastWeeklyDigestAtUtc == null || p.LastWeeklyDigestAtUtc < cutoff)
                    && p.Children.Any())
            .Select(p => p.Id)
            .ToListAsync(ct);

        var sentCount = 0;
        foreach (var id in dueParentIds)
        {
            try { if (await SendForParentAsync(id, ct)) sentCount++; }
            catch (Exception ex) { _logger.LogWarning(ex, "Digest failed for parent {Id}", id); }
        }
        return sentCount;
    }

    private static string BuildChildBlock(string name, int lessonsCompleted, int watchMinutes,
        int assessmentsTaken, double avgScore, int homeworkSubmitted, int pastPapers,
        int streak, int shields)
    {
        string safe(string s) => WebUtility.HtmlEncode(s);
        return $@"
<div style='background:#F8FAFC;border-radius:12px;padding:20px;margin:16px 0'>
    <h3 style='margin:0 0 14px;color:#0F172A'>👤 {safe(name)}</h3>
    <table cellspacing='0' cellpadding='0' style='width:100%;border-collapse:collapse'>
        <tr>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0'>📚 دروس مكتملة هذا الأسبوع</td>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0;text-align:left;font-weight:800'>{lessonsCompleted}</td>
        </tr>
        <tr>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0'>⏱ دقائق المشاهدة</td>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0;text-align:left;font-weight:800'>{watchMinutes}</td>
        </tr>
        <tr>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0'>🎯 اختبارات</td>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0;text-align:left;font-weight:800'>{assessmentsTaken} (متوسط: {avgScore:F1})</td>
        </tr>
        <tr>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0'>📝 واجبات سُلِّمت</td>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0;text-align:left;font-weight:800'>{homeworkSubmitted}</td>
        </tr>
        <tr>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0'>📜 امتحانات سابقة</td>
            <td style='padding:10px 0;border-bottom:1px solid #E2E8F0;text-align:left;font-weight:800'>{pastPapers}</td>
        </tr>
        <tr>
            <td style='padding:10px 0'>🔥 سلسلة الأيام</td>
            <td style='padding:10px 0;text-align:left;font-weight:800'>{streak} يوم — 🛡 {shields}</td>
        </tr>
    </table>
</div>";
    }

    private static string BuildEmail(string parentName, List<string> childBlocks)
    {
        var children = string.Join("\n", childBlocks);
        var name = WebUtility.HtmlEncode(parentName);
        return $@"<!doctype html>
<html dir='rtl' lang='ar'>
<body style='margin:0;background:#F8FAFC;font-family:system-ui,-apple-system,sans-serif;color:#0F172A'>
<div style='max-width:640px;margin:24px auto;background:#fff;border-radius:12px;padding:32px;box-shadow:0 1px 3px rgba(0,0,0,.05)'>
  <div style='font-weight:800;color:#5B4FE5;font-size:20px;margin-bottom:6px'>FSEdu</div>
  <h2 style='margin:0 0 6px;font-size:22px'>📊 تقرير الأسبوع</h2>
  <p style='color:#475569;margin:0 0 18px'>أهلًا {name}، إليك ملخّص نشاط أبنائك خلال الأسبوع الماضى.</p>
  {children}
  <p style='margin:24px 0 0'>
    <a href='https://fsedu.local/parent/children' style='background:#5B4FE5;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700'>افتح لوحة ولى الأمر ←</a>
  </p>
  <hr style='border:none;border-top:1px solid #E2E8F0;margin:24px 0'/>
  <small style='color:#94A3B8'>يتم إرسال هذا التقرير أسبوعيًا. لتعطيله، انتقل لإعدادات حسابك.</small>
</div>
</body></html>";
    }
}
