namespace FSEdu.Domain.Notifications;

// One row per (UserId, Category) — absence of a row means "default enabled"
// for that category. The user toggles a category off to opt out of all
// notifications of related types (both in-app storage AND push delivery).
public sealed class NotificationPreference
{
    public Guid UserId { get; private set; }
    public string Category { get; private set; } = default!;
    public bool Enabled { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private NotificationPreference() { }

    public NotificationPreference(Guid userId, string category, bool enabled)
    {
        UserId = userId;
        Category = category;
        Enabled = enabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Set(bool enabled)
    {
        Enabled = enabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

// Maps fine-grained notification "type" strings to user-facing categories.
// Used by NotificationService to filter and by the preferences UI to group.
public static class NotificationCategories
{
    public const string Subscriptions = "subscriptions";   // payment.*, subscription.*
    public const string Courses = "courses";               // course.approved/rejected/announcement
    public const string LiveSessions = "live_sessions";    // session.*
    public const string QA = "qa";                         // lesson.question, lesson.answer
    public const string Achievements = "achievements";     // badge.*, leaderboard.*
    public const string Support = "support";               // ticket.*
    public const string Announcements = "announcements";   // admin.broadcast

    public static readonly string[] All =
    {
        Subscriptions, Courses, LiveSessions, QA, Achievements, Support, Announcements
    };

    public static string CategoryOf(string type)
    {
        if (string.IsNullOrEmpty(type)) return Announcements;
        if (type.StartsWith("payment.") || type.StartsWith("subscription.")) return Subscriptions;
        if (type.StartsWith("course.")) return Courses;
        if (type.StartsWith("session.")) return LiveSessions;
        if (type.StartsWith("lesson.question") || type.StartsWith("lesson.answer")) return QA;
        if (type.StartsWith("badge.") || type.StartsWith("leaderboard.")) return Achievements;
        if (type.StartsWith("ticket.")) return Support;
        if (type.StartsWith("admin.")) return Announcements;
        if (type.StartsWith("teacher.")) return Announcements; // admin-targeted
        return Announcements;
    }
}
