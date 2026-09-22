namespace FSEdu.Application.Abstractions;

public interface INotificationService
{
    /// <summary>Send an in-app notification to a single user.</summary>
    Task SendAsync(Guid userId, string type, string title, string body,
                   object? data = null, CancellationToken ct = default);

    /// <summary>Send to multiple users.</summary>
    Task SendBulkAsync(IEnumerable<Guid> userIds, string type, string title, string body,
                       object? data = null, CancellationToken ct = default);
}

/// <summary>Standard notification type codes.</summary>
public static class NotificationTypes
{
    public const string PaymentApproved = "payment.approved";
    public const string PaymentRejected = "payment.rejected";
    public const string SubscriptionExpiring = "subscription.expiring";

    public const string CourseApproved = "course.approved";
    public const string CourseRejected = "course.rejected";
    public const string CourseSubmittedForReview = "course.submitted";

    public const string TeacherApproved = "teacher.approved";

    public const string TicketCreated = "ticket.created";
    public const string TicketReplied = "ticket.replied";
    public const string TicketResolved = "ticket.resolved";

    public const string AssessmentGraded = "assessment.graded";

    public const string Welcome = "welcome";
    public const string General = "general";
}
