using System.Text.RegularExpressions;
using FSEdu.Shared.Kernel.Primitives;
using FSEdu.Shared.Kernel.Results;

namespace FSEdu.Domain.Common;

public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Error.Validation("EMAIL.EMPTY", "البريد الإلكتروني مطلوب", "Email is required");

        var trimmed = raw.Trim().ToLowerInvariant();
        if (!EmailRegex().IsMatch(trimmed))
            return Error.Validation("EMAIL.INVALID", "صيغة البريد الإلكتروني غير صحيحة", "Invalid email format");

        return new Email(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
