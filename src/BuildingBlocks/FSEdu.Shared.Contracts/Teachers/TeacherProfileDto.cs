using FSEdu.Shared.Contracts.Courses;

namespace FSEdu.Shared.Contracts.Teachers;

public sealed record TeacherProfileDto(
    Guid Id,
    string FullName,
    string? Bio,
    int YearsOfExperience,
    decimal RatingAvg,
    int RatingsCount,
    bool Verified,
    string[] SubjectNames,
    string[] RegionNames,
    TeacherQualificationDto[] Qualifications,
    TeacherProfileStatsDto Stats,
    CourseListItemDto[] PublishedCourses,
    string? AvatarUrl = null,
    bool TwoFactorEnabled = false
);

public sealed record TeacherQualificationDto(
    long Id,
    string Title,
    string Institution,
    int Year
);

// Self-edit ─────────────────────────────────
public sealed record UpdateTeacherProfileRequest(string? Bio, int YearsOfExperience);
public sealed record AddTeacherQualificationRequest(string Title, string Institution, int Year);

public sealed record TeacherProfileStatsDto(
    int PublishedCoursesCount,
    int TotalLessons,
    int TotalEnrollments,
    int RecordingsCount,
    int LiveSessionsHosted
);
