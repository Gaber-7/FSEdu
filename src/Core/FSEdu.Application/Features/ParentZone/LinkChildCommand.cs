using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.ParentZone;

public sealed record LinkChildCommand(string StudentPhone, string Relation) : ICommand;

public sealed class LinkChildValidator : AbstractValidator<LinkChildCommand>
{
    public LinkChildValidator()
    {
        RuleFor(x => x.StudentPhone).NotEmpty().Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("صيغة رقم هاتف الطالب غير صحيحة");

        RuleFor(x => x.Relation).NotEmpty()
            .Must(r => r is "Father" or "Mother" or "Guardian")
            .WithMessage("العلاقة يجب أن تكون: أب، أم، أو ولي أمر");
    }
}

public sealed class LinkChildHandler : ICommandHandler<LinkChildCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LinkChildHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LinkChildCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Authentication required");

        var parentId = _currentUser.UserId.Value;

        var parent = await _db.Parents.FirstOrDefaultAsync(p => p.Id == parentId, ct);
        if (parent is null)
            return Error.Forbidden("AUTH.NOT_PARENT", "هذا الحساب ليس ولي أمر", "Not a parent account");

        var phoneVo = PhoneNumber.Create(request.StudentPhone);
        if (phoneVo.IsFailure) return phoneVo.Error;

        var student = await _db.Students
            .FirstOrDefaultAsync(s => s.Phone.Value == phoneVo.Value.Value, ct);
        if (student is null)
            return Error.NotFound("STUDENT.NOT_FOUND", "لا يوجد طالب بهذا الرقم", "Student not found");

        var alreadyLinked = await _db.ParentStudentLinks
            .AnyAsync(l => l.ParentId == parentId && l.StudentId == student.Id, ct);
        if (alreadyLinked)
            return Error.Conflict("LINK.EXISTS", "تم ربط هذا الطالب بحسابك مسبقًا", "Already linked");

        var relation = Enum.Parse<Relation>(request.Relation);
        var hasPrimary = await _db.ParentStudentLinks.AnyAsync(l => l.ParentId == parentId, ct);

        parent.LinkChild(student.Id, relation, isPrimary: !hasPrimary);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
