using FSEdu.Domain.Academic;
using FSEdu.Domain.Billing;
using FSEdu.Domain.Common;
using FSEdu.Domain.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSEdu.Persistence.Seeding;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");

        await db.Database.MigrateAsync(ct);

        await SeedStagesAsync(db, logger, ct);
        await SeedSubjectsAsync(db, logger, ct);
        await SeedRegionsAsync(db, logger, ct);
        await SeedSchoolsAsync(db, logger, ct);
        await SeedPlansAsync(db, logger, ct);
        await SeedBadgesAsync(db, logger, ct);
        await SeedPricingAsync(db, logger, ct);

        logger.LogInformation("✓ Seed completed");
    }

    private static async Task SeedPricingAsync(ApplicationDbContext db, ILogger log, CancellationToken ct)
    {
        var anySubjectPriced = await db.Subjects.AnyAsync(s => s.MonthlyPriceEgp > 0, ct);
        if (anySubjectPriced) return;

        // Price matrix per grade (LevelOrder)
        // LevelOrder: 11..16 = Primary grades 1..6 | 21..23 = Preparatory grades 1..3
        var pricing = new Dictionary<int, (decimal monthly, decimal term, decimal stageBundle)>
        {
            { 11, (80m,  320m,  700m ) }, // الأول ابتدائي
            { 12, (90m,  360m,  800m ) }, // الثاني ابتدائي
            { 13, (100m, 400m,  900m ) }, // الثالث ابتدائي
            { 14, (110m, 440m,  1000m) }, // الرابع ابتدائي
            { 15, (120m, 480m,  1100m) }, // الخامس ابتدائي
            { 16, (130m, 520m,  1200m) }, // السادس ابتدائي
            { 21, (150m, 600m,  1400m) }, // الأول إعدادي
            { 22, (170m, 680m,  1600m) }, // الثاني إعدادي
            { 23, (200m, 800m,  1900m) }, // الثالث إعدادي
        };

        var stages = await db.Stages
            .Include(s => s.Subjects)
            .Where(s => s.ParentStageId != null)
            .ToListAsync(ct);

        foreach (var stage in stages)
        {
            if (!pricing.TryGetValue(stage.LevelOrder, out var tier)) continue;

            stage.SetFullTermPrice(tier.stageBundle);

            foreach (var subject in stage.Subjects)
                subject.SetPricing(tier.monthly, tier.term);
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("✓ Pricing seeded for {Count} grade stages", stages.Count);
    }

    private static async Task SeedStagesAsync(ApplicationDbContext db, ILogger log, CancellationToken ct)
    {
        if (await db.Stages.AnyAsync(ct)) return;

        var primary = new Stage("المرحلة الابتدائية", 10, "Primary");
        var prep = new Stage("المرحلة الإعدادية", 20, "Preparatory");

        db.Stages.AddRange(primary, prep);
        await db.SaveChangesAsync(ct);

        var primaryGrades = new[]
        {
            ("الصف الأول الابتدائي", "Grade 1", 11),
            ("الصف الثاني الابتدائي", "Grade 2", 12),
            ("الصف الثالث الابتدائي", "Grade 3", 13),
            ("الصف الرابع الابتدائي", "Grade 4", 14),
            ("الصف الخامس الابتدائي", "Grade 5", 15),
            ("الصف السادس الابتدائي", "Grade 6", 16),
        };

        foreach (var (ar, en, order) in primaryGrades)
            db.Stages.Add(new Stage(ar, order, en, primary.Id));

        var prepGrades = new[]
        {
            ("الصف الأول الإعدادي", "Grade 7", 21),
            ("الصف الثاني الإعدادي", "Grade 8", 22),
            ("الصف الثالث الإعدادي", "Grade 9", 23),
        };

        foreach (var (ar, en, order) in prepGrades)
            db.Stages.Add(new Stage(ar, order, en, prep.Id));

        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded {Count} stages", primaryGrades.Length + prepGrades.Length + 2);
    }

    private static async Task SeedSubjectsAsync(ApplicationDbContext db, ILogger log, CancellationToken ct)
    {
        if (await db.Subjects.AnyAsync(ct)) return;

        var stages = await db.Stages.Where(s => s.ParentStageId != null).ToListAsync(ct);

        var primarySubjects = new[]
        {
            ("اللغة العربية", "Arabic", "#E63946", "book-open"),
            ("الرياضيات", "Mathematics", "#2A9D8F", "calculator"),
            ("اللغة الإنجليزية", "English", "#457B9D", "language"),
            ("العلوم", "Science", "#8AB17D", "flask"),
            ("الدراسات الاجتماعية", "Social Studies", "#E76F51", "globe"),
            ("الدين الإسلامي", "Islamic Education", "#06A77D", "moon"),
            ("الحاسب الآلي", "ICT", "#6A4C93", "monitor"),
        };

        var prepSubjects = new[]
        {
            ("اللغة العربية", "Arabic", "#E63946", "book-open"),
            ("الرياضيات", "Mathematics", "#2A9D8F", "calculator"),
            ("اللغة الإنجليزية", "English", "#457B9D", "language"),
            ("العلوم", "Science", "#8AB17D", "flask"),
            ("الدراسات الاجتماعية", "Social Studies", "#E76F51", "globe"),
            ("الدين الإسلامي", "Islamic Education", "#06A77D", "moon"),
            ("الحاسب الآلي", "ICT", "#6A4C93", "monitor"),
            ("اللغة الفرنسية", "French", "#1D3557", "language"),
        };

        foreach (var stage in stages)
        {
            var subjects = stage.LevelOrder < 20 ? primarySubjects : prepSubjects;
            foreach (var (ar, en, color, icon) in subjects)
                db.Subjects.Add(new Subject(stage.Id, ar, en, icon, color));
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded subjects for {Count} stages", stages.Count);
    }

    private static async Task SeedRegionsAsync(ApplicationDbContext db, ILogger log, CancellationToken ct)
    {
        if (await db.Regions.AnyAsync(ct)) return;

        var regions = new[]
        {
            ("القاهرة", "Cairo", "CAI"),
            ("الجيزة", "Giza", "GIZ"),
            ("القليوبية", "Qalyubia", "QAL"),
            ("الإسكندرية", "Alexandria", "ALX"),
            ("الدقهلية", "Dakahlia", "DAK"),
            ("الشرقية", "Sharqia", "SHR"),
            ("المنوفية", "Monufia", "MNF"),
            ("الغربية", "Gharbia", "GHR"),
            ("كفر الشيخ", "Kafr El Sheikh", "KFS"),
            ("البحيرة", "Beheira", "BHR"),
            ("دمياط", "Damietta", "DMT"),
            ("بورسعيد", "Port Said", "PTS"),
            ("الإسماعيلية", "Ismailia", "ISM"),
            ("السويس", "Suez", "SUZ"),
            ("شمال سيناء", "North Sinai", "NSI"),
            ("جنوب سيناء", "South Sinai", "SSI"),
            ("الفيوم", "Faiyum", "FYM"),
            ("بني سويف", "Beni Suef", "BNS"),
            ("المنيا", "Minya", "MNY"),
            ("أسيوط", "Asyut", "AST"),
            ("سوهاج", "Sohag", "SHG"),
            ("قنا", "Qena", "QNA"),
            ("الأقصر", "Luxor", "LXR"),
            ("أسوان", "Aswan", "ASW"),
            ("البحر الأحمر", "Red Sea", "RDS"),
            ("الوادي الجديد", "New Valley", "NVL"),
            ("مطروح", "Matruh", "MTR"),
        };

        foreach (var (ar, en, code) in regions)
            db.Regions.Add(new Region(ar, code, en));

        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded {Count} regions", regions.Length);
    }

    private static async Task SeedSchoolsAsync(ApplicationDbContext db, ILogger log, CancellationToken ct)
    {
        if (await db.Schools.AnyAsync(ct)) return;

        var cairo = await db.Regions.FirstAsync(r => r.Code == "CAI", ct);
        var giza = await db.Regions.FirstAsync(r => r.Code == "GIZ", ct);
        var alex = await db.Regions.FirstAsync(r => r.Code == "ALX", ct);

        var schools = new[]
        {
            new School("مدرسة النيل الدولية", cairo.Id, SchoolType.International),
            new School("مدرسة المستقبل الخاصة", cairo.Id, SchoolType.Private),
            new School("مدرسة الحرية الابتدائية", cairo.Id, SchoolType.Public),
            new School("مدرسة الأهرام للغات", giza.Id, SchoolType.Language),
            new School("مدرسة 6 أكتوبر النموذجية", giza.Id, SchoolType.Private),
            new School("مدرسة الإسكندرية الحديثة", alex.Id, SchoolType.Private),
            new School("مدرسة سابا باشا الابتدائية", alex.Id, SchoolType.Public),
        };

        db.Schools.AddRange(schools);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded {Count} sample schools", schools.Length);
    }

    private static async Task SeedPlansAsync(ApplicationDbContext db, ILogger log, CancellationToken ct)
    {
        if (await db.Plans.AnyAsync(ct)) return;

        db.Plans.AddRange(
            new Plan("basic-monthly", "الباقة الأساسية - شهري", BillingInterval.Monthly, 199m),
            new Plan("basic-yearly", "الباقة الأساسية - سنوي", BillingInterval.Yearly, 1999m),
            new Plan("pro-monthly", "الباقة المميزة - شهري", BillingInterval.Monthly, 399m),
            new Plan("pro-yearly", "الباقة المميزة - سنوي", BillingInterval.Yearly, 3999m),
            new Plan("premium-monthly", "الباقة الذهبية - شهري", BillingInterval.Monthly, 599m),
            new Plan("premium-yearly", "الباقة الذهبية - سنوي", BillingInterval.Yearly, 5999m)
        );

        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded 6 plans");
    }

    private static async Task SeedBadgesAsync(ApplicationDbContext db, ILogger log, CancellationToken ct)
    {
        if (await db.Badges.AnyAsync(ct)) return;

        db.Badges.AddRange(
            new Badge("first-lesson", "الخطوة الأولى", "/badges/first-lesson.svg",
                "{\"lessons_completed\":1}"),
            new Badge("week-streak", "مواظب لأسبوع", "/badges/week-streak.svg",
                "{\"streak_days\":7}"),
            new Badge("month-streak", "مواظب لشهر", "/badges/month-streak.svg",
                "{\"streak_days\":30}"),
            new Badge("perfect-score", "الدرجة الكاملة", "/badges/perfect-score.svg",
                "{\"perfect_scores\":1}"),
            new Badge("top-ten", "ضمن العشرة الأوائل", "/badges/top-ten.svg",
                "{\"leaderboard_rank\":10}"),
            new Badge("helpful", "طالب متعاون", "/badges/helpful.svg",
                "{\"community_helps\":5}")
        );

        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded 6 badges");
    }
}
