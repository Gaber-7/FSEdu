namespace FSEdu.Shared.Contracts.Payments;

public sealed record PaymentInstructionsDto(
    string InstaPayNumber,
    string InstaPayLink,
    string VodafoneCashNumber,
    string BankName,
    string BankAccount,
    string BankIban,
    string AccountHolder
);

public sealed record SubscribeWithPaymentRequest(
    string Type,              // "SubjectMonthly" | "SubjectTerm" | "StageFullTerm"
    int? SubjectId,
    int? StageId,
    string? Term,             // "First" | "Second" | "Annual"
    string PaymentMethod,     // "InstaPay" | "VodafoneCash" | "BankTransfer" | "Cash"
    string? ReferenceNumber,
    string? ReceiptImageUrl,
    string? SenderName,
    string? Notes,
    string? CouponCode = null
);

public sealed record ValidateCouponRequest(string Code, decimal Amount);

public sealed record ValidateCouponResponse(
    bool Valid,
    string? Code,
    decimal OriginalAmount,
    decimal Discount,
    decimal FinalAmount,
    string? Message
);

public sealed record SubscribeWithPaymentResponse(
    Guid SubscriptionId,
    Guid PaymentId,
    decimal AmountDue,
    string Currency,
    string Status,          // "PendingPayment"
    string Message
);

public sealed record UploadReceiptResponse(string Url);

// ─── Admin ─────────────────────────────────────
public sealed record PendingPaymentDto(
    Guid PaymentId,
    Guid SubscriptionId,
    Guid UserId,
    string UserName,
    string UserPhone,
    string SubscriptionType,
    string? SubjectName,
    string? StageName,
    string? Term,
    decimal Amount,
    string Currency,
    string? PaymentMethod,
    string? ReferenceNumber,
    string? ReceiptImageUrl,
    string? SenderName,
    string? Notes,
    DateTime? SubmittedAtUtc
);

public sealed record ApprovePaymentRequest(string? Note);
public sealed record RejectPaymentRequest(string Reason);
