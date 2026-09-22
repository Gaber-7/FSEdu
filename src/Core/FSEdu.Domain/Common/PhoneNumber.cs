using System.Text.RegularExpressions;
using FSEdu.Shared.Kernel.Primitives;
using FSEdu.Shared.Kernel.Results;

namespace FSEdu.Domain.Common;

public sealed partial class PhoneNumber : ValueObject
{
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    public static Result<PhoneNumber> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Error.Validation("PHONE.EMPTY", "رقم الهاتف مطلوب", "Phone number is required");

        var normalized = Regex.Replace(raw.Trim(), @"[\s\-\(\)]", "");
        if (normalized.StartsWith("00")) normalized = "+" + normalized[2..];

        if (!E164Regex().IsMatch(normalized))
            return Error.Validation("PHONE.INVALID",
                "رقم الهاتف غير صالح. يجب أن يبدأ بـ + ورمز الدولة",
                "Invalid phone format. Must start with + and country code");

        return new PhoneNumber(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164Regex();
}
