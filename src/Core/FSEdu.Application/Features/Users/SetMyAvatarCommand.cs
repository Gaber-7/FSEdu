using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Users;

// Updates the current user's avatar URL. The upload endpoint stores the
// file under wwwroot/uploads/avatars and passes the relative URL here.
// Pass an empty / null URL to remove the avatar.
public sealed record SetMyAvatarCommand(string? AvatarUrl) : ICommand;

public sealed class SetMyAvatarValidator : AbstractValidator<SetMyAvatarCommand>
{
    public SetMyAvatarValidator()
    {
        RuleFor(x => x.AvatarUrl).MaximumLength(500);
    }
}

public sealed class SetMyAvatarHandler : ICommandHandler<SetMyAvatarCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SetMyAvatarHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(SetMyAvatarCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Error.NotFound("USER.NOT_FOUND", "المستخدم غير موجود", "Not found");

        var url = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        // UpdateProfile signature: (fullName, avatarUrl, gender, birthDate) — preserve everything else.
        user.UpdateProfile(user.FullName, url, user.Gender, user.BirthDate);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
