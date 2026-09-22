using FSEdu.Domain.Billing;
using FSEdu.Domain.Common;
using FSEdu.Domain.Courses;
using FSEdu.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Api.Seeding;

public static class DevContentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevContentSeeder");

        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.Phone.Value == "+20115155693");
        var student = await db.Students.FirstOrDefaultAsync(s => s.Phone.Value == "+20115155691");

        if (teacher is null || student is null)
        {
            log.LogInformation("Content seed skipped — test users not found");
            return;
        }

        // ───────────────── Ensure StageFullTerm Subscription for Student ─────────────────
        var hasStageBundle = await db.Subscriptions.AnyAsync(s =>
            s.UserId == student.Id
            && s.Type == SubscriptionType.StageFullTerm
            && s.Status == SubscriptionStatus.Active);

        if (!hasStageBundle)
        {
            var studentStage = await db.Stages.FirstOrDefaultAsync(s => s.Id == student.StageId);
            if (studentStage is not null)
            {
                var stageBundlePrice = studentStage.FullTermPriceEgp ?? 1000m;
                var stageFullTerm = Subscription.CreateStageFullTerm(
                    Guid.NewGuid(),
                    student.Id,
                    studentStage.Id,
                    AcademicTerm.First,
                    DateTime.UtcNow.AddDays(-5),
                    DateTime.UtcNow.AddMonths(5),
                    stageBundlePrice,
                    PaymentProvider.Manual,
                    "DEV_AUTO_SEED");

                db.Subscriptions.Add(stageFullTerm);
                await db.SaveChangesAsync();

                log.LogWarning("🎟️ [DEV] Student '{Name}' subscribed to StageFullTerm for '{Stage}'",
                    student.FullName, studentStage.NameAr);
            }
        }

        // ───────────────── Courses for Student's Stage ─────────────────
        var stageHasCourses = await db.Courses.AnyAsync(c => c.StageId == student.StageId);
        if (stageHasCourses)
        {
            log.LogInformation("Student's stage already has courses — skipping content seed");
            return;
        }

        var subjects = await db.Subjects
            .Where(s => s.StageId == student.StageId && s.IsActive)
            .ToListAsync();

        var sampleVideos = new[]
        {
            "https://www.youtube.com/watch?v=BELlZKpi1Zs",
            "https://www.youtube.com/watch?v=w2Qzb_zG3gI",
            "https://www.youtube.com/watch?v=4xqAo4XBq9c",
            "https://www.youtube.com/watch?v=QXeEoD0pB3E",
        };

        var coursesCount = 0;
        foreach (var subject in subjects)
        {
            var course = new Course(
                Guid.NewGuid(),
                teacher.Id,
                subject.Id,
                subject.StageId,
                $"{subject.NameAr} — دورة شاملة",
                subject.TermPriceEgp,
                AcademicTerm.First);

            course.UpdateDetails(
                $"{subject.NameAr} — دورة شاملة",
                $"دورة تعليمية شاملة في مادة {subject.NameAr} مع شرح مبسّط ومذكرات وامتحانات دورية تغطي المنهج كاملاً.",
                null,
                subject.TermPriceEgp);

            course.Publish();
            db.Courses.Add(course);

            var ch1 = new Chapter(Guid.NewGuid(), course.Id, "الوحدة الأولى: المقدمة والأساسيات", 1);
            db.Chapters.Add(ch1);

            var l11 = new Lesson(Guid.NewGuid(), ch1.Id, "الدرس 1: التعريف بالمادة", LessonType.Recorded, 1);
            l11.SetVideo(sampleVideos[0], null, 600);
            l11.MarkFreePreview();
            db.Lessons.Add(l11);

            var l12 = new Lesson(Guid.NewGuid(), ch1.Id, "الدرس 2: المفاهيم الأساسية", LessonType.Recorded, 2);
            l12.SetVideo(sampleVideos[1], null, 900);
            db.Lessons.Add(l12);

            var ch2 = new Chapter(Guid.NewGuid(), course.Id, "الوحدة الثانية: التطبيقات العملية", 2);
            db.Chapters.Add(ch2);

            var l21 = new Lesson(Guid.NewGuid(), ch2.Id, "الدرس 3: أمثلة محلولة", LessonType.Recorded, 1);
            l21.SetVideo(sampleVideos[2], null, 1200);
            db.Lessons.Add(l21);

            var l22 = new Lesson(Guid.NewGuid(), ch2.Id, "الدرس 4: تدريبات عامة", LessonType.Recorded, 2);
            l22.SetVideo(sampleVideos[3], null, 1500);
            db.Lessons.Add(l22);

            coursesCount++;
        }

        await db.SaveChangesAsync();
        log.LogWarning("📚 [DEV] Seeded {Count} published courses for stage '{Stage}'",
            coursesCount, subjects.FirstOrDefault()?.StageId);
    }
}
