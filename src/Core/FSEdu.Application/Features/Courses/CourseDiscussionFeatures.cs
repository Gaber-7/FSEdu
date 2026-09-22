using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// ─── Helper: who can interact with a course's discussions ──────
file static class DiscussionPolicy
{
    // Anyone with course access (subject subscription, teacher, admin)
    // or where the course has any free-preview lessons can READ + POST.
    public static async Task<(bool canPost, bool isCourseTeacher)> ResolveAsync(
        IApplicationDbContext db, IAccessPolicy access, ICurrentUser current,
        Guid courseId, CancellationToken ct)
    {
        if (current.UserId is null) return (false, false);
        var userId = current.UserId.Value;
        if (current.IsInRole("Admin")) return (true, false);

        var teacherId = await db.Courses.Where(c => c.Id == courseId)
            .Select(c => (Guid?)c.TeacherId).FirstOrDefaultAsync(ct);
        if (teacherId == userId) return (true, true);

        var course = await db.Courses.Where(c => c.Id == courseId)
            .Select(c => new { c.SubjectId }).FirstOrDefaultAsync(ct);
        if (course is null) return (false, false);

        var hasAccess = await access.CanAccessSubjectAsync(userId, course.SubjectId, ct);
        return (hasAccess, false);
    }
}

// ─── List discussions ────────────────────────────────
public sealed record GetCourseDiscussionsQuery(Guid CourseId, int Take = 50) : IQuery<DiscussionsResponse>;

public sealed class GetCourseDiscussionsHandler
    : IQueryHandler<GetCourseDiscussionsQuery, DiscussionsResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetCourseDiscussionsHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<DiscussionsResponse>> Handle(
        GetCourseDiscussionsQuery request, CancellationToken ct)
    {
        var courseExists = await _db.Courses.AnyAsync(c => c.Id == request.CourseId, ct);
        if (!courseExists)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        var (canPost, isCourseTeacher) = await DiscussionPolicy
            .ResolveAsync(_db, _access, _currentUser, request.CourseId, ct);

        var list = await _db.CourseDiscussions
            .Where(d => d.CourseId == request.CourseId)
            .OrderByDescending(d => d.Pinned)
            .ThenByDescending(d => d.LastActivityAtUtc)
            .Take(Math.Clamp(request.Take, 1, 100))
            .Select(d => new DiscussionListItemDto(
                d.Id, d.CourseId, d.AuthorUserId, d.AuthorName,
                d.Title,
                d.Body.Length > 160 ? d.Body.Substring(0, 160) + "…" : d.Body,
                d.CreatedAtUtc, d.LastActivityAtUtc,
                d.RepliesCount, d.Pinned, d.Locked))
            .ToListAsync(ct);

        var total = await _db.CourseDiscussions.CountAsync(d => d.CourseId == request.CourseId, ct);

        return Result.Success(new DiscussionsResponse(total, canPost, isCourseTeacher, list));
    }
}

// ─── Get one discussion with replies ─────────────────
public sealed record GetDiscussionQuery(Guid DiscussionId) : IQuery<DiscussionDetailDto>;

public sealed class GetDiscussionHandler : IQueryHandler<GetDiscussionQuery, DiscussionDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetDiscussionHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<DiscussionDetailDto>> Handle(GetDiscussionQuery request, CancellationToken ct)
    {
        var d = await _db.CourseDiscussions
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == request.DiscussionId, ct);
        if (d is null)
            return Error.NotFound("DISCUSSION.NOT_FOUND", "النقاش غير موجود", "Not found");

        var (canReply, isCourseTeacher) = await DiscussionPolicy
            .ResolveAsync(_db, _access, _currentUser, d.CourseId, ct);

        var uid = _currentUser.UserId;
        var isAdmin = _currentUser.IsInRole("Admin");
        var isAuthor = uid == d.AuthorUserId;
        var canModerate = isAdmin || isCourseTeacher;

        // If thread is locked, only moderators can reply.
        if (d.Locked && !canModerate) canReply = false;

        var replies = await _db.CourseDiscussionReplies
            .Where(r => r.DiscussionId == d.Id)
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => new DiscussionReplyDto(
                r.Id, r.AuthorUserId, r.AuthorName, r.Body, r.CreatedAtUtc,
                uid == r.AuthorUserId,
                isAdmin || isCourseTeacher || uid == r.AuthorUserId))
            .ToArrayAsync(ct);

        return Result.Success(new DiscussionDetailDto(
            d.Id, d.CourseId, d.Course.Title,
            d.AuthorUserId, d.AuthorName,
            d.Title, d.Body,
            d.CreatedAtUtc, d.UpdatedAtUtc,
            d.Pinned, d.Locked,
            canReply, canModerate, isAuthor,
            replies));
    }
}

// ─── Create discussion ───────────────────────────────
public sealed record CreateDiscussionCommand(Guid CourseId, string Title, string Body) : ICommand<Guid>;

public sealed class CreateDiscussionValidator : AbstractValidator<CreateDiscussionCommand>
{
    public CreateDiscussionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(255);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(5).MaximumLength(4000);
    }
}

public sealed class CreateDiscussionHandler : ICommandHandler<CreateDiscussionCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public CreateDiscussionHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<Guid>> Handle(CreateDiscussionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var (canPost, _) = await DiscussionPolicy
            .ResolveAsync(_db, _access, _currentUser, request.CourseId, ct);
        if (!canPost)
            return Error.Forbidden("DISCUSSION.NO_ACCESS", "ليس لديك صلاحية النقاش في هذه الدورة", "No access");

        var uid = _currentUser.UserId.Value;
        var name = await _db.Users.Where(u => u.Id == uid).Select(u => u.FullName)
            .FirstOrDefaultAsync(ct) ?? "مستخدم";

        var d = new CourseDiscussion(Guid.NewGuid(), request.CourseId, uid, name, request.Title, request.Body);
        _db.CourseDiscussions.Add(d);
        await _db.SaveChangesAsync(ct);
        return Result.Success(d.Id);
    }
}

// ─── Reply to discussion ─────────────────────────────
public sealed record PostDiscussionReplyCommand(Guid DiscussionId, string Body) : ICommand<Guid>;

public sealed class PostDiscussionReplyValidator : AbstractValidator<PostDiscussionReplyCommand>
{
    public PostDiscussionReplyValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MinimumLength(2).MaximumLength(4000);
    }
}

public sealed class PostDiscussionReplyHandler : ICommandHandler<PostDiscussionReplyCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public PostDiscussionReplyHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<Guid>> Handle(PostDiscussionReplyCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var d = await _db.CourseDiscussions.FirstOrDefaultAsync(x => x.Id == request.DiscussionId, ct);
        if (d is null)
            return Error.NotFound("DISCUSSION.NOT_FOUND", "النقاش غير موجود", "Not found");

        var (canPost, isCourseTeacher) = await DiscussionPolicy
            .ResolveAsync(_db, _access, _currentUser, d.CourseId, ct);
        if (!canPost)
            return Error.Forbidden("DISCUSSION.NO_ACCESS", "ليس لديك صلاحية النقاش", "No access");

        var isAdmin = _currentUser.IsInRole("Admin");
        if (d.Locked && !(isAdmin || isCourseTeacher))
            return Error.Forbidden("DISCUSSION.LOCKED", "هذا النقاش مغلق", "Locked");

        var uid = _currentUser.UserId.Value;
        var name = await _db.Users.Where(u => u.Id == uid).Select(u => u.FullName)
            .FirstOrDefaultAsync(ct) ?? "مستخدم";

        var reply = new CourseDiscussionReply(Guid.NewGuid(), d.Id, uid, name, request.Body);
        _db.CourseDiscussionReplies.Add(reply);
        d.RecordReplyAdded();
        await _db.SaveChangesAsync(ct);
        return Result.Success(reply.Id);
    }
}

// ─── Delete discussion ───────────────────────────────
public sealed record DeleteDiscussionCommand(Guid DiscussionId) : ICommand;

public sealed class DeleteDiscussionHandler : ICommandHandler<DeleteDiscussionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteDiscussionHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteDiscussionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var d = await _db.CourseDiscussions
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == request.DiscussionId, ct);
        if (d is null)
            return Error.NotFound("DISCUSSION.NOT_FOUND", "النقاش غير موجود", "Not found");

        var uid = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");
        var isAuthor = d.AuthorUserId == uid;
        var isCourseTeacher = d.Course.TeacherId == uid;
        if (!isAdmin && !isAuthor && !isCourseTeacher)
            return Error.Forbidden("DISCUSSION.FORBIDDEN", "غير مسموح بالحذف", "Forbidden");

        _db.CourseDiscussions.Remove(d);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Delete reply ────────────────────────────────────
public sealed record DeleteDiscussionReplyCommand(Guid ReplyId) : ICommand;

public sealed class DeleteDiscussionReplyHandler : ICommandHandler<DeleteDiscussionReplyCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteDiscussionReplyHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteDiscussionReplyCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var r = await _db.CourseDiscussionReplies
            .Include(x => x.Discussion).ThenInclude(d => d.Course)
            .FirstOrDefaultAsync(x => x.Id == request.ReplyId, ct);
        if (r is null)
            return Error.NotFound("REPLY.NOT_FOUND", "الرد غير موجود", "Not found");

        var uid = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");
        var isAuthor = r.AuthorUserId == uid;
        var isCourseTeacher = r.Discussion.Course.TeacherId == uid;
        if (!isAdmin && !isAuthor && !isCourseTeacher)
            return Error.Forbidden("REPLY.FORBIDDEN", "غير مسموح بالحذف", "Forbidden");

        _db.CourseDiscussionReplies.Remove(r);
        r.Discussion.RecordReplyRemoved();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Pin / Lock toggles (teacher/admin) ──────────────
public sealed record ToggleDiscussionPinCommand(Guid DiscussionId, bool Pinned) : ICommand;

public sealed class ToggleDiscussionPinHandler : ICommandHandler<ToggleDiscussionPinCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ToggleDiscussionPinHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(ToggleDiscussionPinCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var d = await _db.CourseDiscussions.Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == request.DiscussionId, ct);
        if (d is null)
            return Error.NotFound("DISCUSSION.NOT_FOUND", "النقاش غير موجود", "Not found");

        var uid = _currentUser.UserId.Value;
        if (!_currentUser.IsInRole("Admin") && d.Course.TeacherId != uid)
            return Error.Forbidden("MODERATE.FORBIDDEN", "للمدرّس أو الأدمن فقط", "Forbidden");

        if (request.Pinned) d.Pin(); else d.Unpin();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record ToggleDiscussionLockCommand(Guid DiscussionId, bool Locked) : ICommand;

public sealed class ToggleDiscussionLockHandler : ICommandHandler<ToggleDiscussionLockCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ToggleDiscussionLockHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(ToggleDiscussionLockCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var d = await _db.CourseDiscussions.Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == request.DiscussionId, ct);
        if (d is null)
            return Error.NotFound("DISCUSSION.NOT_FOUND", "النقاش غير موجود", "Not found");

        var uid = _currentUser.UserId.Value;
        if (!_currentUser.IsInRole("Admin") && d.Course.TeacherId != uid)
            return Error.Forbidden("MODERATE.FORBIDDEN", "للمدرّس أو الأدمن فقط", "Forbidden");

        if (request.Locked) d.Lock(); else d.Unlock();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
