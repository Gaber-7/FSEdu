using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Contracts.Subscriptions;

namespace FSEdu.Shared.Contracts.Dashboard;

// ─── Student Dashboard ──────────────────────────
public sealed record StudentDashboardDto(
    string FullName,
    string StageName,
    int XpPoints,
    int CurrentStreakDays,
    int LongestStreakDays,
    int AccessibleCoursesCount,
    int AccessibleLessonsCount,
    MySubscriptionDto? ActiveBundleSubscription,
    int IndividualSubjectSubscriptionsCount,
    DateTime? SubscriptionExpiresAt,
    CourseListItemDto[] RecentCourses
);

// ─── Teacher Dashboard ──────────────────────────
public sealed record TeacherDashboardDto(
    string FullName,
    bool Verified,
    int YearsOfExperience,
    decimal RatingAvg,
    int RatingsCount,
    int TotalCourses,
    int PublishedCourses,
    int PendingCourses,
    int DraftCourses,
    int TotalLessons,
    int TotalEnrollments,
    CourseListItemDto[] RecentCourses,
    TeacherLiveAnalyticsDto LiveAnalytics
);

public sealed record TeacherLiveAnalyticsDto(
    int TotalSessionsHosted,
    int SessionsThisWeek,
    int LiveNow,
    int ScheduledUpcoming,
    int RecordingsAvailable,
    int UniqueStudentsAttended,
    int TotalAttendanceMinutes,
    int AvgAttendancePerSession,
    int OpenTicketsAssigned,
    // Qualified attendance = students who watched MORE than 50% of the session's
    // planned duration. Used to compute teacher compensation fairly.
    int QualifiedAttendanceTotal,
    int AvgQualifiedPerSession,
    TeacherSessionRowDto[] RecentSessions
);

public sealed record TeacherSessionRowDto(
    Guid Id,
    string Title,
    string Status,                  // Scheduled | Live | Ended | Cancelled
    DateTime ScheduledAtUtc,
    DateTime? EndedAtUtc,
    int DurationMinutes,
    int AttendanceCount,
    int QualifiedAttendanceCount,   // attendees with TotalDurationSec ≥ 50% of session duration
    bool HasRecording
);

// ─── Parent Dashboard (enhanced child card) ──────
public sealed record ParentDashboardDto(
    string FullName,
    int ChildrenCount,
    ChildProgressDto[] Children
);

public sealed record ChildProgressDto(
    Guid StudentId,
    string FullName,
    string Phone,
    string StageName,
    string Relation,
    bool IsPrimary,
    int XpPoints,
    int CurrentStreak,
    bool HasActiveSubscription,
    string? ActiveSubscriptionLabel,
    DateTime? SubscriptionExpiresAt,
    int AccessibleCoursesCount
);

// ─── Deep child detail (for parent's /parent/child/{id} view) ──
public sealed record ChildDetailDto(
    Guid StudentId,
    string FullName,
    string Phone,
    string? Email,
    string StageName,
    string RegionName,
    string? AvatarUrl,
    int XpPoints,
    int CurrentStreak,
    int LongestStreak,
    int BadgesCount,
    int? GlobalRank,

    // Subscriptions
    ChildSubscriptionDto[] Subscriptions,

    // Progress
    int TotalEnrolledCourses,
    int LessonsCompletedTotal,
    int LessonsViewedToday,
    int AssessmentsSubmittedTotal,
    decimal AvgAssessmentScore,

    // Recent activity
    ChildRecentLessonDto[] RecentLessons,
    ChildRecentAttemptDto[] RecentAttempts,
    ChildUpcomingSessionDto[] UpcomingSessions
);

public sealed record ChildSubscriptionDto(
    Guid Id,
    string Type,
    string? SubjectName,
    string? StageName,
    string Status,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    decimal AmountPaid,
    string Currency
);

public sealed record ChildRecentLessonDto(
    Guid LessonId,
    string LessonTitle,
    string CourseTitle,
    decimal WatchedPct,
    bool Completed,
    DateTime LastViewedAtUtc
);

public sealed record ChildRecentAttemptDto(
    Guid AttemptId,
    string AssessmentTitle,
    decimal Score,
    decimal TotalMarks,
    DateTime SubmittedAtUtc
);

public sealed record ChildUpcomingSessionDto(
    Guid SessionId,
    string Title,
    string? SubjectName,
    string TeacherName,
    DateTime ScheduledAtUtc,
    int DurationMinutes,
    string Status
);
