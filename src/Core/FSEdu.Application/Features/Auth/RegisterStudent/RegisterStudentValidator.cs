using FluentValidation;

namespace FSEdu.Application.Features.Auth.RegisterStudent;

public sealed class RegisterStudentValidator : AbstractValidator<RegisterStudentCommand>
{
    public RegisterStudentValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب")
            .MinimumLength(3).WithMessage("الاسم يجب ألا يقل عن 3 أحرف")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب")
            .Matches(@"^\+[1-9]\d{7,14}$").WithMessage("صيغة رقم الهاتف غير صحيحة (يجب أن يبدأ بـ + ورمز الدولة)");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة")
            .MinimumLength(8).WithMessage("كلمة المرور يجب ألا تقل عن 8 أحرف")
            .Matches(@"\d").WithMessage("كلمة المرور يجب أن تحتوي على رقم على الأقل");

        RuleFor(x => x.StageId).GreaterThan(0).WithMessage("المرحلة الدراسية مطلوبة");
        RuleFor(x => x.RegionId).GreaterThan(0).WithMessage("المحافظة مطلوبة");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!)
                .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
        });

        When(x => !string.IsNullOrWhiteSpace(x.ParentPhone), () =>
        {
            RuleFor(x => x.ParentPhone!)
                .Matches(@"^\+[1-9]\d{7,14}$").WithMessage("صيغة رقم هاتف ولي الأمر غير صحيحة");
        });
    }
}
