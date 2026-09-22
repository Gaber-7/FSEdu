namespace FSEdu.Shared.Contracts.Courses;

// ─── Teacher ─────────────────────────────────────
public sealed record CreateCourseRequest(
    int SubjectId,
    int StageId,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    string Term, // "First" | "Second" | "Annual"
    string? PreviewVideoUrl = null
);

public sealed record AddChapterRequest(string Title, int OrderNum);

public sealed record AddLessonRequest(
    string Title,
    string Type,           // "Recorded" | "Live" | "Document" | "Quiz"
    int DurationSeconds,
    string? VideoUrl,
    DateTime? ScheduledAtUtc,
    bool IsFreePreview,
    int OrderNum
);

public sealed record CourseListItemDto(
    Guid Id,
    string Title,
    string? ThumbnailUrl,
    int SubjectId,
    string SubjectName,
    int StageId,
    string StageName,
    string Status,
    int EnrollmentCount,
    decimal RatingAvg,
    int LessonsCount
);

public sealed record CourseDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    string? PreviewVideoUrl,
    int SubjectId,
    string SubjectName,
    string? SubjectColor,
    int StageId,
    string StageName,
    Guid TeacherId,
    string TeacherName,
    string Status,
    string Term,
    int EnrollmentCount,
    decimal RatingAvg,
    bool HasAccess,
    ChapterDto[] Chapters
);

public sealed record ChapterDto(
    Guid Id,
    string Title,
    int OrderNum,
    LessonSummaryDto[] Lessons
);

public sealed record LessonSummaryDto(
    Guid Id,
    string Title,
    string Type,
    int DurationSeconds,
    bool IsFreePreview,
    int OrderNum,
    DateTime? ScheduledAtUtc
);

public sealed record LessonPlaybackDto(
    Guid Id,
    string Title,
    string Type,
    int DurationSeconds,
    string? VideoUrl,
    string? VideoDrmKeyId,
    Guid CourseId,
    string CourseTitle,
    int OrderNum,
    bool HasAccess,
    bool IsFreePreview = false,
    bool IsCourseTeacher = false
);

// ─── Student progress ────────────────────────────
public sealed record CourseProgressDto(
    Guid CourseId,
    string Title,
    string? ThumbnailUrl,
    int SubjectId,
    string SubjectName,
    int TotalLessons,
    int CompletedLessons,
    int ProgressPct,
    Guid? LastLessonId,
    string? LastLessonTitle,
    int LastPositionSec,
    DateTime? LastViewedAtUtc
);

// ─── Lesson Q&A ─────────────────────────────────
public sealed record AskLessonQuestionRequest(string Body);
public sealed record AnswerLessonQuestionRequest(string Body);

public sealed record LessonQuestionDto(
    Guid Id,
    Guid LessonId,
    Guid AskedByUserId,
    string AskedByName,
    string Body,
    DateTime CreatedAtUtc,
    string? AnswerBody,
    string? AnsweredByName,
    DateTime? AnsweredAtUtc,
    int VotesCount = 0,
    bool MyVoted = false
);

public sealed record VoteQuestionResponse(bool NowActive, int VotesCount);

public sealed record LessonQuestionsResponse(
    int TotalCount,
    int UnansweredCount,
    bool CanAsk,
    bool CanAnswer,
    List<LessonQuestionDto> Questions
);

// Teacher's cross-course Q&A inbox row (lesson + course context for each question)
public sealed record TeacherInboxQuestionDto(
    Guid Id,
    Guid LessonId,
    string LessonTitle,
    Guid CourseId,
    string CourseTitle,
    Guid AskedByUserId,
    string AskedByName,
    string Body,
    DateTime CreatedAtUtc,
    string? AnswerBody,
    string? AnsweredByName,
    DateTime? AnsweredAtUtc
);

public sealed record TeacherInboxResponse(
    int TotalCount,
    int UnansweredCount,
    List<TeacherInboxQuestionDto> Questions
);

// ─── Course reviews ──────────────────────────────
public sealed record SubmitReviewRequest(int Rating, string? Comment);

public sealed record CourseReviewDto(
    Guid StudentId,
    string StudentName,
    int Rating,
    string? Comment,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public sealed record CourseReviewsResponse(
    decimal AverageRating,
    int TotalCount,
    int[] StarBreakdown,        // [n1, n2, n3, n4, n5]
    CourseReviewDto? MyReview,
    bool CanReview,
    List<CourseReviewDto> Reviews
);

// ─── Lesson notes (private to the student) ───────
public sealed record LessonNoteDto(
    Guid LessonId,
    string Body,
    DateTime? UpdatedAtUtc
);

public sealed record SaveLessonNoteRequest(string Body);

// All my notes (across courses) for the "My Notes" study page.
public sealed record MyNoteRowDto(
    Guid LessonId,
    string LessonTitle,
    Guid CourseId,
    string CourseTitle,
    string SubjectName,
    string Body,
    DateTime UpdatedAtUtc
);

public sealed record MyNotesResponse(
    int TotalCount,
    int CoursesWithNotesCount,
    List<MyNoteRowDto> Notes
);

// ─── Course announcements ────────────────────────
public sealed record CourseAnnouncementDto(
    Guid Id,
    Guid CourseId,
    Guid AuthorUserId,
    string AuthorName,
    string Title,
    string Body,
    bool Pinned,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public sealed record CourseAnnouncementsResponse(
    bool CanPost,
    int TotalCount,
    List<CourseAnnouncementDto> Announcements
);

public sealed record PostAnnouncementRequest(string Title, string Body, bool Pinned);

// ─── Lesson attachments ──────────────────────────
public sealed record LessonAttachmentDto(
    Guid Id,
    Guid LessonId,
    string Title,
    string FileUrl,
    string FileType,
    long SizeBytes
);

public sealed record AddLessonAttachmentRequest(
    string Title,
    string FileUrl,
    string FileType,
    long SizeBytes
);

public sealed record UploadAttachmentResponse(string Url, string FileType, long SizeBytes);

// ─── Lesson markers (private timestamp bookmarks) ────
public sealed record LessonMarkerDto(
    Guid Id,
    Guid LessonId,
    int PositionSec,
    string? Label,
    DateTime CreatedAtUtc
);

public sealed record AddLessonMarkerRequest(int PositionSec, string? Label);

// ─── Lesson reactions ────────────────────────────
public sealed record LessonReactionsDto(
    int HelpfulCount,
    int ConfusingCount,
    int LovedCount,
    bool MyHelpful,
    bool MyConfusing,
    bool MyLoved
);

public sealed record ToggleReactionRequest(string Type);  // "Helpful" | "Confusing" | "Loved"
public sealed record ToggleReactionResponse(bool NowActive, LessonReactionsDto Totals);

// ─── Course Discussions ──────────────────────────
public sealed record DiscussionListItemDto(
    Guid Id,
    Guid CourseId,
    Guid AuthorUserId,
    string AuthorName,
    string Title,
    string BodyExcerpt,
    DateTime CreatedAtUtc,
    DateTime LastActivityAtUtc,
    int RepliesCount,
    bool Pinned,
    bool Locked
);

public sealed record DiscussionsResponse(
    int TotalCount,
    bool CanPost,
    bool IsCourseTeacher,
    List<DiscussionListItemDto> Items
);

public sealed record DiscussionReplyDto(
    Guid Id,
    Guid AuthorUserId,
    string AuthorName,
    string Body,
    DateTime CreatedAtUtc,
    bool IsAuthor,
    bool CanDelete
);

public sealed record DiscussionDetailDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    Guid AuthorUserId,
    string AuthorName,
    string Title,
    string Body,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    bool Pinned,
    bool Locked,
    bool CanReply,
    bool CanModerate,
    bool IsAuthor,
    DiscussionReplyDto[] Replies
);

public sealed record CreateDiscussionRequest(string Title, string Body);
public sealed record PostReplyRequest(string Body);

// ─── Course insights (teacher analytics) ─────────
public sealed record CourseInsightsDto(
    Guid CourseId,
    string CourseTitle,
    int EnrollmentCount,
    int TotalLessons,
    decimal AvgWatchedPct,
    decimal CompletionRate,
    int TotalQuestions,
    int UnansweredQuestions,
    int TotalReactions,
    int HelpfulCount,
    int ConfusingCount,
    int LovedCount,
    decimal RatingAvg,
    int RatingsCount,
    LessonInsightDto[] Lessons
);

// ─── Class roster (teacher view) ─────────────────
public sealed record RosterStudentDto(
    Guid StudentId,
    string FullName,
    string Phone,
    string? Email,
    string StageName,
    string? AvatarUrl,
    DateTime EnrolledAtUtc,
    int LessonsCompletedInCourse,
    decimal ProgressPct,
    DateTime? LastViewedAtUtc,
    decimal? LastAssessmentScorePct
);

public sealed record CourseRosterDto(
    Guid CourseId,
    string CourseTitle,
    int TotalEnrolled,
    int TotalLessons,
    RosterStudentDto[] Students
);

public sealed record SendCourseMessageRequest(
    Guid[] StudentIds,         // empty array = "all enrolled"
    string Title,
    string Body,
    string? Url
);

public sealed record SendCourseMessageResponse(int Recipients);

// ─── Student Detailed Stats ──────────────────────
public sealed record StudentDetailedStatsDto(
    // Watching time (computed from WatchedPct × DurationSeconds across all lessons)
    int TotalWatchedMinutes,
    int MinutesLast7Days,
    int MinutesLast30Days,

    // Assessment performance
    int AssessmentsSubmitted,
    decimal CorrectAnswerPct,           // average score / total marks across all attempts
    int HighScoreCount,                  // attempts with ≥90%
    int LowScoreCount,                   // attempts with <50%

    // Breakdowns
    StatsBySubjectDto[] BySubject,
    StatsByDateDto[] ByDate              // last 30 days, one row per day with activity
);

public sealed record StatsBySubjectDto(
    int SubjectId,
    string SubjectName,
    string? ColorHex,
    int WatchedMinutes,
    int LessonsCompleted,
    int AssessmentsSubmitted,
    decimal AvgScorePct
);

public sealed record StatsByDateDto(
    DateTime DateLocal,
    int WatchedMinutes,
    int LessonsViewed
);

// ─── Teacher Activity Feed ───────────────────────
public sealed record TeacherActivityItemDto(
    string Kind,         // "enrollment" | "question" | "review" | "attempt" | "discussion" | "reaction"
    string Emoji,
    string Title,
    string Subtitle,
    Guid CourseId,
    string CourseTitle,
    string? StudentName,
    DateTime AtUtc,
    string? Url,
    int Priority         // higher = more urgent (e.g., unanswered question, low rating)
);

public sealed record LessonInsightDto(
    Guid LessonId,
    string LessonTitle,
    string ChapterTitle,
    int OrderNum,
    int ViewCount,
    int CompletionCount,
    decimal AvgWatchedPct,
    int QuestionsCount,
    int UnansweredCount,
    int HelpfulCount,
    int ConfusingCount,
    int LovedCount
);

// ─── Student weekly schedule ─────────────────────
public sealed record ScheduleItemDto(
    string Kind,                // "live" | "assessment"
    Guid Id,
    string Title,
    string? SubjectName,
    string? CourseTitle,
    string? TeacherName,
    DateTime AtUtc,             // start time for live, due (or available-from) for assessment
    int? DurationMinutes,       // null for assessments
    string? Status,              // live status string, or "available"/"upcoming" for assessments
    string? RoomId               // for live sessions
);

public sealed record StudentScheduleResponse(
    DateTime FromUtc,
    DateTime ToUtc,
    List<ScheduleItemDto> Items
);

// ─── AI-style suggestions widget ────────────────
public sealed record SuggestionDto(
    string Kind,         // "streak_risk" | "stale" | "almost_done" | "low_score" | "upcoming_live"
                         // | "subscription_expiring" | "first_notes" | "first_assessment"
    string Emoji,
    string Title,
    string Body,
    string? ActionLabel,
    string? ActionUrl,
    int Priority         // higher = more urgent, used for ordering
);

// ─── Daily mission (today's goals) ───────────────
public sealed record DailyMissionGoalDto(
    string Code,           // "watch_lesson" | "complete_lesson" | "submit_assessment" | "earn_xp"
    string Title,
    string Emoji,
    int Target,
    int Progress,
    bool Done
);

public sealed record DailyMissionDto(
    DateTime DateLocal,
    int CompletedCount,
    int TotalCount,
    bool AllDone,
    int CurrentStreakDays,
    DailyMissionGoalDto[] Goals
);

// ─── Activity feed ──────────────────────────────
public sealed record ActivityItemDto(
    string Kind,                // "lesson" | "badge" | "assessment" | "question" | "review" | "marker" | "subscription"
    string Emoji,
    string Title,
    string? Subtitle,
    DateTime AtUtc,
    string? Url
);

// ─── Bookmarks ───────────────────────────────────
public sealed record BookmarkedCourseDto(
    Guid CourseId,
    string Title,
    string SubjectName,
    string StageName,
    string TeacherName,
    string? ThumbnailUrl,
    int LessonsCount,
    decimal RatingAvg,
    DateTime SavedAtUtc
);

public sealed record ToggleBookmarkResponse(bool IsBookmarked);

// ─── Continue Watching ───────────────────────────
public sealed record ContinueWatchingItemDto(
    Guid LessonId,
    string LessonTitle,
    Guid CourseId,
    string CourseTitle,
    string SubjectName,
    string? ThumbnailUrl,
    int LastPositionSec,
    int DurationSeconds,
    decimal WatchedPct,
    DateTime LastViewedAtUtc
);

public sealed record CourseCertificateDto(
    Guid CourseId,
    string CourseTitle,
    string SubjectName,
    string StageName,
    string TeacherName,
    string StudentName,
    int TotalLessons,
    DateTime CompletedAtUtc,
    string CertificateNumber
);

public sealed record MyCertificateRowDto(
    Guid CourseId,
    string CourseTitle,
    string SubjectName,
    string? SubjectColor,
    string? ThumbnailUrl,
    string CertificateNumber,
    DateTime IssuedAtUtc,
    decimal FinalGrade
);

public sealed record StudentProgressDto(
    int TotalCourses,
    int TotalLessonsAvailable,
    int TotalLessonsCompleted,
    int OverallPct,
    int CurrentStreakDays,
    int LongestStreakDays,
    int XpPoints,
    int BadgesEarned,
    Guid? ResumeLessonId,
    Guid? ResumeCourseId,
    string? ResumeLessonTitle,
    string? ResumeCourseTitle,
    List<CourseProgressDto> Courses
);
