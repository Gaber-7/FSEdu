namespace FSEdu.Shared.Kernel.Results;

public sealed record Error(string Code, string MessageAr, string MessageEn = "", ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty, string.Empty, ErrorType.Failure);

    public static Error NotFound(string code, string messageAr, string messageEn = "") =>
        new(code, messageAr, messageEn, ErrorType.NotFound);

    public static Error Validation(string code, string messageAr, string messageEn = "") =>
        new(code, messageAr, messageEn, ErrorType.Validation);

    public static Error Conflict(string code, string messageAr, string messageEn = "") =>
        new(code, messageAr, messageEn, ErrorType.Conflict);

    public static Error Unauthorized(string code, string messageAr, string messageEn = "") =>
        new(code, messageAr, messageEn, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string messageAr, string messageEn = "") =>
        new(code, messageAr, messageEn, ErrorType.Forbidden);
}

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}
