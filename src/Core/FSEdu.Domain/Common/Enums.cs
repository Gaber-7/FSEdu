namespace FSEdu.Domain.Common;

public enum UserStatus { Pending = 0, Active = 1, Suspended = 2, Deactivated = 3 }

public enum Gender { Male = 1, Female = 2 }

public enum Relation { Father = 1, Mother = 2, Guardian = 3 }

public enum CourseStatus { Draft = 0, PendingReview = 1, Published = 2, Archived = 3, Rejected = 4 }

public enum LessonType { Recorded = 1, Live = 2, Document = 3, Quiz = 4, Assignment = 5 }

public enum LiveSessionStatus { Scheduled = 1, Live = 2, Ended = 3, Cancelled = 4 }

public enum AssessmentType { Assignment = 1, Quiz = 2, Exam = 3 }

public enum AttemptStatus { InProgress = 1, Submitted = 2, Graded = 3, Expired = 4 }

public enum QuestionType { MultipleChoice = 1, TrueFalse = 2, ShortAnswer = 3, Essay = 4, Match = 5, FillBlank = 6 }

public enum Difficulty { Easy = 1, Medium = 2, Hard = 3 }

public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Cancelled = 4,
    Expired = 5,
    PendingPayment = 6  // Subscription created but awaiting admin review of receipt
}

public enum PaymentMethod
{
    InstaPay = 1,
    VodafoneCash = 2,
    BankTransfer = 3,
    Cash = 4               // Paid in-person at the center; admin confirms receipt
}

public enum SubscriptionType
{
    SubjectMonthly  = 1,  // Single subject, monthly recurring
    SubjectTerm     = 2,  // Single subject, one term
    StageFullTerm   = 3   // All subjects of a stage, one term
}

public enum BillingInterval { Monthly = 1, Quarterly = 2, Yearly = 3 }

public enum PaymentStatus
{
    Pending = 1,        // Created but no receipt uploaded yet
    AwaitingReview = 2, // Receipt uploaded, awaiting admin review
    Succeeded = 3,      // Admin approved
    Failed = 4,         // Admin rejected
    Refunded = 5
}

public enum PaymentProvider { Stripe = 1, Paymob = 2, Fawry = 3, Manual = 99 }

public enum TicketStatus { Open = 1, Answered = 2, Resolved = 3, Closed = 4 }

public enum TicketPriority { Low = 1, Normal = 2, High = 3, Urgent = 4 }

public enum NotificationChannel { InApp = 1, Email = 2, Sms = 3, Push = 4 }

public enum CommunityRole { Member = 1, Moderator = 2, Supervisor = 3 }

public enum SchoolType { Public = 1, Private = 2, International = 3, Language = 4 }

public enum AcademicTerm { First = 1, Second = 2, Annual = 3 }
