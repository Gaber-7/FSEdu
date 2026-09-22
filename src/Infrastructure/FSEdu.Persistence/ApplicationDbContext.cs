using FSEdu.Application.Abstractions;
using FSEdu.Domain.Academic;
using FSEdu.Domain.Assessments;
using FSEdu.Domain.Billing;
using FSEdu.Domain.Courses;
using FSEdu.Domain.Engagement;
using FSEdu.Domain.LiveClassroom;
using FSEdu.Domain.Notifications;
using FSEdu.Domain.Support;
using FSEdu.Domain.Users;
using FSEdu.Persistence.Interceptors;
using FSEdu.Shared.Kernel.Time;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser? _currentUser;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDateTimeProvider clock,
        ICurrentUser? currentUser = null) : base(options)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    // Academic
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<School> Schools => Set<School>();

    // Users
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<TeacherRegion> TeacherRegions => Set<TeacherRegion>();
    public DbSet<TeacherQualification> TeacherQualifications => Set<TeacherQualification>();

    // Courses
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonAttachment> LessonAttachments => Set<LessonAttachment>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<CourseReview> CourseReviews => Set<CourseReview>();
    public DbSet<LessonQuestion> LessonQuestions => Set<LessonQuestion>();
    public DbSet<LessonNote> LessonNotes => Set<LessonNote>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<Homework> Homeworks => Set<Homework>();
    public DbSet<HomeworkSubmission> HomeworkSubmissions => Set<HomeworkSubmission>();
    public DbSet<CourseAnnouncement> CourseAnnouncements => Set<CourseAnnouncement>();
    public DbSet<CourseBookmark> CourseBookmarks => Set<CourseBookmark>();
    public DbSet<LessonMarker> LessonMarkers => Set<LessonMarker>();
    public DbSet<LessonReaction> LessonReactions => Set<LessonReaction>();
    public DbSet<CourseDiscussion> CourseDiscussions => Set<CourseDiscussion>();
    public DbSet<CourseDiscussionReply> CourseDiscussionReplies => Set<CourseDiscussionReply>();
    public DbSet<LessonQuestionVote> LessonQuestionVotes => Set<LessonQuestionVote>();

    // Live
    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
    public DbSet<LiveAttendance> LiveAttendance => Set<LiveAttendance>();

    // Assessments
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
    public DbSet<PastPaper> PastPapers => Set<PastPaper>();
    public DbSet<PastPaperQuestion> PastPaperQuestions => Set<PastPaperQuestion>();
    public DbSet<PastPaperAttempt> PastPaperAttempts => Set<PastPaperAttempt>();

    // Billing
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<SalesCampaign> SalesCampaigns => Set<SalesCampaign>();
    public DbSet<DiscountRule> DiscountRules => Set<DiscountRule>();

    // Engagement
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<StudentBadge> StudentBadges => Set<StudentBadge>();
    public DbSet<WeeklyChallenge> WeeklyChallenges => Set<WeeklyChallenge>();
    public DbSet<DailyChallenge> DailyChallenges => Set<DailyChallenge>();
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<CommunityMember> CommunityMembers => Set<CommunityMember>();

    // Support
    public DbSet<AskTicket> AskTickets => Set<AskTicket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    // Audit
    public DbSet<FSEdu.Domain.Audit.AuditEntry> AuditEntries => Set<FSEdu.Domain.Audit.AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("edu");
        modelBuilder.UseCollation("Arabic_CI_AS");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new AuditingInterceptor(_clock, _currentUser));
        optionsBuilder.AddInterceptors(new SoftDeleteInterceptor(_clock, _currentUser));
        base.OnConfiguring(optionsBuilder);
    }
}
