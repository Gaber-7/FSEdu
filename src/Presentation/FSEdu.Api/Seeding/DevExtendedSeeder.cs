using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Domain.Common;
using FSEdu.Domain.LiveClassroom;
using FSEdu.Domain.Notifications;
using FSEdu.Domain.Support;
using FSEdu.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Api.Seeding;

// Adds rich test data for live classroom, tickets, notifications, payments,
// and gamification. Idempotent — safe to run on every dev startup.
public static class DevExtendedSeeder
{
    // Public BigBuckBunny mp4 — reliable test stream
    private const string SampleRecordingUrl =
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var gamification = scope.ServiceProvider.GetRequiredService<IGamificationService>();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevExtendedSeeder");

        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.Phone.Value == "+20115155693");
        var student = await db.Students.FirstOrDefaultAsync(s => s.Phone.Value == "+20115155691");
        if (teacher is null || student is null)
        {
            log.LogInformation("Extended seed skipped — dev users not yet created");
            return;
        }

        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.StageId == student.StageId && s.IsActive);
        if (subject is null) { log.LogInformation("No subject found for student stage"); return; }

        await SeedLiveSessions(db, teacher.Id, student.StageId, subject.Id, log);
        await SeedExpiringSubscription(db, student.Id, student.StageId, log);
        await SeedTicket(db, student.Id, teacher.Id, subject.Id, log);
        await SeedPendingPayment(db, student.Id, log);
        await SeedNotifications(db, student.Id, log);
        await SeedGamification(gamification, student.Id, log);

        log.LogWarning("══════════════════════════════════════════════════════");
        log.LogWarning("✅ Extended dev seed complete — UI ready for testing");
        log.LogWarning("══════════════════════════════════════════════════════");
    }

    // ─── Live Sessions ────────────────────────────────────
    private static async Task SeedLiveSessions(ApplicationDbContext db, Guid teacherId,
                                                int stageId, int subjectId, ILogger log)
    {
        var marker = "[DEV-SEED] ";

        // Always refresh dev-seeded sessions so the "Live now" + "ended yesterday"
        // timestamps stay sensible across restarts. Real teacher-scheduled sessions
        // (without the marker) are untouched.
        var stale = await db.LiveSessions.Where(s => s.Title.StartsWith(marker)).ToListAsync();
        if (stale.Count > 0)
        {
            db.LiveSessions.RemoveRange(stale);
            await db.SaveChangesAsync();
            log.LogInformation("Removed {Count} stale dev-seed live sessions", stale.Count);
        }

        // 1) Upcoming scheduled session — in 2 days
        var scheduled = new LiveSession(
            Guid.NewGuid(), teacherId,
            marker + "مراجعة عامة على الوحدة الثالثة",
            subjectId, stageId,
            $"room_{Guid.NewGuid():N}".Substring(0, 24),
            DateTime.UtcNow.AddDays(2), 60,
            description: "حصة مراجعة شاملة قبل امتحان الوحدة الثالثة.");
        db.LiveSessions.Add(scheduled);

        // 2) Currently live session — started 5 minutes ago
        var liveSession = new LiveSession(
            Guid.NewGuid(), teacherId,
            marker + "حصة مباشرة الآن — الجبر التفاعلي",
            subjectId, stageId,
            $"room_{Guid.NewGuid():N}".Substring(0, 24),
            DateTime.UtcNow.AddMinutes(-5), 90,
            description: "حلّ تمارين الجبر بشكل تفاعلي مع الطلاب.");
        liveSession.Start();
        db.LiveSessions.Add(liveSession);

        // 3) Ended session with recording — yesterday
        var ended = new LiveSession(
            Guid.NewGuid(), teacherId,
            marker + "محاضرة الفصل الأول — مكتملة",
            subjectId, stageId,
            $"room_{Guid.NewGuid():N}".Substring(0, 24),
            DateTime.UtcNow.AddDays(-1), 75,
            description: "محاضرة كاملة عن الفصل الأول مع تسجيل متاح للمشاهدة.");
        ended.Start();
        ended.End();
        ended.AttachRecording(SampleRecordingUrl);
        db.LiveSessions.Add(ended);

        await db.SaveChangesAsync();
        log.LogWarning("📅 [DEV] Seeded 3 live sessions (1 scheduled, 1 live, 1 ended+recording)");
    }

    // ─── Subscription expiring in 2 days ────────────────────
    private static async Task SeedExpiringSubscription(ApplicationDbContext db,
                                                       Guid studentId, int stageId, ILogger log)
    {
        var alreadySeeded = await db.Subscriptions.AnyAsync(s =>
            s.UserId == studentId
            && s.Type == SubscriptionType.SubjectMonthly
            && s.EndsAtUtc < DateTime.UtcNow.AddDays(3)
            && s.EndsAtUtc > DateTime.UtcNow);
        if (alreadySeeded) return;

        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.StageId == stageId && s.IsActive);
        if (subject is null) return;

        var price = subject.MonthlyPriceEgp > 0 ? subject.MonthlyPriceEgp : 100m;
        // Pick startsAt so AddMonths(1) lands ~2 days from now → triggers "expiring soon" warning
        var startsAt = DateTime.UtcNow.AddMonths(-1).AddDays(2);
        var sub = Subscription.CreateSubjectMonthly(
            Guid.NewGuid(), studentId, subject.Id, stageId,
            startsAt, price, PaymentProvider.Manual, "DEV_EXPIRING");
        sub.Activate();
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();
        log.LogWarning("⏳ [DEV] Seeded subscription expiring in ~2 days (triggers warning)");
    }

    // ─── One open + one resolved ticket ─────────────────────
    private static async Task SeedTicket(ApplicationDbContext db,
                                         Guid studentId, Guid teacherId, int subjectId, ILogger log)
    {
        if (await db.AskTickets.AnyAsync(t => t.StudentId == studentId)) return;

        // Open ticket
        var openTicket = new AskTicket(Guid.NewGuid(), studentId, subjectId, TicketPriority.Normal);
        openTicket.AddMessage(studentId, "أستاذ، عندي سؤال في الدرس الثاني — مش فاهم خطوة التحليل.");
        db.AskTickets.Add(openTicket);

        // Resolved ticket with reply
        var resolvedTicket = new AskTicket(Guid.NewGuid(), studentId, subjectId, TicketPriority.High);
        resolvedTicket.AddMessage(studentId, "كيف أحضّر نفسي لامتحان آخر الترم؟");
        resolvedTicket.Assign(teacherId);
        resolvedTicket.AddMessage(teacherId,
            "ركز على المراجعة الشاملة لكل وحدة، وحلّ امتحانات السنين السابقة. وأي سؤال احنا هنا.");
        resolvedTicket.AddMessage(studentId, "شكرًا أستاذ، هحاول أنظم وقتي.");
        resolvedTicket.Resolve();
        db.AskTickets.Add(resolvedTicket);

        await db.SaveChangesAsync();
        log.LogWarning("💬 [DEV] Seeded 2 tickets (1 open, 1 resolved)");
    }

    // ─── A pending payment for admin review ─────────────────
    private static async Task SeedPendingPayment(ApplicationDbContext db, Guid studentId, ILogger log)
    {
        if (await db.Payments.AnyAsync(p => p.UserId == studentId
                                         && p.Status == PaymentStatus.AwaitingReview)) return;

        // Create a PendingPayment subscription and a matching payment
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.IsActive);
        if (subject is null) return;

        var pendingSub = Subscription.CreateSubjectMonthly(
            Guid.NewGuid(), studentId, subject.Id, subject.StageId,
            DateTime.UtcNow,
            subject.MonthlyPriceEgp > 0 ? subject.MonthlyPriceEgp : 100m,
            PaymentProvider.Manual, "DEV_PENDING");
        db.Subscriptions.Add(pendingSub);

        var pay = new Payment(Guid.NewGuid(), pendingSub.Id, studentId,
            subject.MonthlyPriceEgp > 0 ? subject.MonthlyPriceEgp : 100m, "EGP");
        pay.SubmitReceipt(PaymentMethod.InstaPay, "REF-DEV-12345",
            receiptImageUrl: null,
            senderName: "أحمد التجريبي",
            notes: "تحويل تجريبي — لمراجعة الأدمن.");
        db.Payments.Add(pay);

        await db.SaveChangesAsync();
        log.LogWarning("💳 [DEV] Seeded 1 pending payment for admin review");
    }

    // ─── Variety of notifications ───────────────────────────
    private static async Task SeedNotifications(ApplicationDbContext db, Guid studentId, ILogger log)
    {
        if (await db.Notifications.AnyAsync(n => n.UserId == studentId)) return;

        var entries = new (string type, string title, string body)[]
        {
            ("welcome", "🎉 أهلاً بك في FSEdu!",
                "مرحبًا بك في منصتنا التعليمية. ابدأ بتصفح الدورات."),
            ("course.approved", "✅ دورة جديدة متاحة",
                "تمت إضافة دورة شاملة في موادك المشتركة — تصفحها الآن."),
            ("assessment.graded", "📊 تم تصحيح اختبارك",
                "حصلت على درجة 85% في اختبار الوحدة الأولى. مبروك!"),
        };

        foreach (var (type, title, body) in entries)
        {
            db.Notifications.Add(new Notification(
                Guid.NewGuid(), studentId, type, title, body));
        }

        // One older read notification
        var read = new Notification(Guid.NewGuid(), studentId,
            "general", "إعلان", "ستكون هناك حصة مراجعة الأسبوع القادم.");
        read.MarkRead();
        db.Notifications.Add(read);

        await db.SaveChangesAsync();
        log.LogWarning("🔔 [DEV] Seeded 4 notifications (3 unread + 1 read)");
    }

    // ─── Some XP + first-lesson badge ───────────────────────
    private static async Task SeedGamification(IGamificationService gamification, Guid studentId, ILogger log)
    {
        await gamification.AwardXpAsync(studentId, 250, "Welcome bonus + sample activity");
        var newBadge = await gamification.TryAwardBadgeAsync(studentId, BadgeCodes.FirstLesson);
        if (newBadge) log.LogWarning("🏆 [DEV] Awarded 'first-lesson' badge + 250 XP");
    }
}
