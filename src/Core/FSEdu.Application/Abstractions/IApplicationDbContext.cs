using FSEdu.Domain.Academic;
using FSEdu.Domain.Assessments;
using FSEdu.Domain.Billing;
using FSEdu.Domain.Courses;
using FSEdu.Domain.Engagement;
using FSEdu.Domain.LiveClassroom;
using FSEdu.Domain.Notifications;
using FSEdu.Domain.Support;
using FSEdu.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Abstractions;

public interface IApplicationDbContext
{
    // Academic
    DbSet<Stage> Stages { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<Region> Regions { get; }
    DbSet<School> Schools { get; }

    // Users
    DbSet<AppUser> Users { get; }
    DbSet<Student> Students { get; }
    DbSet<Parent> Parents { get; }
    DbSet<Teacher> Teachers { get; }
    DbSet<ParentStudentLink> ParentStudentLinks { get; }
    DbSet<TeacherSubject> TeacherSubjects { get; }
    DbSet<TeacherRegion> TeacherRegions { get; }
    DbSet<TeacherQualification> TeacherQualifications { get; }

    // Courses
    DbSet<Course> Courses { get; }
    DbSet<Chapter> Chapters { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<LessonAttachment> LessonAttachments { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<LessonProgress> LessonProgress { get; }
    DbSet<CourseReview> CourseReviews { get; }
    DbSet<LessonQuestion> LessonQuestions { get; }
    DbSet<LessonNote> LessonNotes { get; }
    DbSet<Certificate> Certificates { get; }
    DbSet<Referral> Referrals { get; }
    DbSet<Homework> Homeworks { get; }
    DbSet<HomeworkSubmission> HomeworkSubmissions { get; }
    DbSet<CourseAnnouncement> CourseAnnouncements { get; }
    DbSet<CourseBookmark> CourseBookmarks { get; }
    DbSet<LessonMarker> LessonMarkers { get; }
    DbSet<LessonReaction> LessonReactions { get; }
    DbSet<CourseDiscussion> CourseDiscussions { get; }
    DbSet<CourseDiscussionReply> CourseDiscussionReplies { get; }
    DbSet<LessonQuestionVote> LessonQuestionVotes { get; }

    // Live
    DbSet<LiveSession> LiveSessions { get; }
    DbSet<LiveAttendance> LiveAttendance { get; }

    // Assessments
    DbSet<Question> Questions { get; }
    DbSet<Assessment> Assessments { get; }
    DbSet<AssessmentQuestion> AssessmentQuestions { get; }
    DbSet<AssessmentAttempt> AssessmentAttempts { get; }
    DbSet<PastPaper> PastPapers { get; }
    DbSet<PastPaperQuestion> PastPaperQuestions { get; }
    DbSet<PastPaperAttempt> PastPaperAttempts { get; }

    // Billing
    DbSet<Plan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<SalesCampaign> SalesCampaigns { get; }
    DbSet<DiscountRule> DiscountRules { get; }

    // Engagement
    DbSet<Badge> Badges { get; }
    DbSet<StudentBadge> StudentBadges { get; }
    DbSet<WeeklyChallenge> WeeklyChallenges { get; }
    DbSet<DailyChallenge> DailyChallenges { get; }
    DbSet<Community> Communities { get; }
    DbSet<CommunityMember> CommunityMembers { get; }

    // Support
    DbSet<AskTicket> AskTickets { get; }
    DbSet<TicketMessage> TicketMessages { get; }

    // Notifications
    DbSet<Notification> Notifications { get; }
    DbSet<PushSubscription> PushSubscriptions { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }

    // Audit
    DbSet<FSEdu.Domain.Audit.AuditEntry> AuditEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
