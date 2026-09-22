using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// ─── Get reactions for a lesson ───────────────────────
public sealed record GetLessonReactionsQuery(Guid LessonId) : IQuery<LessonReactionsDto>;

public sealed class GetLessonReactionsHandler : IQueryHandler<GetLessonReactionsQuery, LessonReactionsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetLessonReactionsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<LessonReactionsDto>> Handle(GetLessonReactionsQuery request, CancellationToken ct)
    {
        var counts = await _db.LessonReactions
            .Where(r => r.LessonId == request.LessonId)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int helpful = counts.FirstOrDefault(c => c.Type == ReactionType.Helpful)?.Count ?? 0;
        int confusing = counts.FirstOrDefault(c => c.Type == ReactionType.Confusing)?.Count ?? 0;
        int loved = counts.FirstOrDefault(c => c.Type == ReactionType.Loved)?.Count ?? 0;

        bool myH = false, myC = false, myL = false;
        if (_currentUser.UserId is Guid uid)
        {
            var mine = await _db.LessonReactions
                .Where(r => r.LessonId == request.LessonId && r.StudentId == uid)
                .Select(r => r.Type)
                .ToListAsync(ct);
            myH = mine.Contains(ReactionType.Helpful);
            myC = mine.Contains(ReactionType.Confusing);
            myL = mine.Contains(ReactionType.Loved);
        }

        return Result.Success(new LessonReactionsDto(helpful, confusing, loved, myH, myC, myL));
    }
}

// ─── Toggle a reaction ────────────────────────────────
public sealed record ToggleLessonReactionCommand(Guid LessonId, string Type)
    : ICommand<ToggleReactionResponse>;

public sealed class ToggleLessonReactionValidator : AbstractValidator<ToggleLessonReactionCommand>
{
    public ToggleLessonReactionValidator()
    {
        RuleFor(x => x.Type).Must(t => t is "Helpful" or "Confusing" or "Loved")
            .WithMessage("نوع التفاعل غير معروف");
    }
}

public sealed class ToggleLessonReactionHandler
    : ICommandHandler<ToggleLessonReactionCommand, ToggleReactionResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public ToggleLessonReactionHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<ToggleReactionResponse>> Handle(
        ToggleLessonReactionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var isStudent = await _db.Students.AnyAsync(s => s.Id == studentId, ct);
        if (!isStudent)
            return Error.Forbidden("REACTIONS.STUDENTS_ONLY", "التفاعلات للطلاب فقط", "Students only");

        var hasAccess = await _access.CanAccessLessonAsync(studentId, request.LessonId, ct);
        if (!hasAccess)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية الوصول لهذا الدرس", "No access");

        var reactionType = Enum.Parse<ReactionType>(request.Type);

        var existing = await _db.LessonReactions
            .FirstOrDefaultAsync(r => r.StudentId == studentId
                                   && r.LessonId == request.LessonId
                                   && r.Type == reactionType, ct);

        bool nowActive;
        if (existing is null)
        {
            _db.LessonReactions.Add(new LessonReaction(studentId, request.LessonId, reactionType));
            nowActive = true;
        }
        else
        {
            _db.LessonReactions.Remove(existing);
            nowActive = false;
        }

        await _db.SaveChangesAsync(ct);

        // Build the fresh totals to return — avoids the client needing a 2nd round-trip.
        var counts = await _db.LessonReactions
            .Where(r => r.LessonId == request.LessonId)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int helpful = counts.FirstOrDefault(c => c.Type == ReactionType.Helpful)?.Count ?? 0;
        int confusing = counts.FirstOrDefault(c => c.Type == ReactionType.Confusing)?.Count ?? 0;
        int loved = counts.FirstOrDefault(c => c.Type == ReactionType.Loved)?.Count ?? 0;

        var mine = await _db.LessonReactions
            .Where(r => r.LessonId == request.LessonId && r.StudentId == studentId)
            .Select(r => r.Type)
            .ToListAsync(ct);

        var totals = new LessonReactionsDto(
            helpful, confusing, loved,
            mine.Contains(ReactionType.Helpful),
            mine.Contains(ReactionType.Confusing),
            mine.Contains(ReactionType.Loved));

        return Result.Success(new ToggleReactionResponse(nowActive, totals));
    }
}
