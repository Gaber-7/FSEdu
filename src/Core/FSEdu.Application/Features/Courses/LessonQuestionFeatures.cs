using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// ─── Ask a question on a lesson ────────────────────────
public sealed record AskLessonQuestionCommand(Guid LessonId, string Body) : ICommand<Guid>;

public sealed class AskLessonQuestionValidator : AbstractValidator<AskLessonQuestionCommand>
{
    public AskLessonQuestionValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MinimumLength(3).MaximumLength(2000);
    }
}

public sealed class AskLessonQuestionHandler : ICommandHandler<AskLessonQuestionCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;
    private readonly INotificationService _notifications;

    public AskLessonQuestionHandler(IApplicationDbContext db, ICurrentUser currentUser,
                                     IAccessPolicy access, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _access = access; _notifications = notifications;
    }

    public async Task<Result<Guid>> Handle(AskLessonQuestionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var hasAccess = await _access.CanAccessLessonAsync(userId, request.LessonId, ct);
        if (!hasAccess)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية الوصول لهذا الدرس", "No access");

        var name = await _db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(ct)
                   ?? "مستخدم";

        var q = new LessonQuestion(Guid.NewGuid(), request.LessonId, userId, name, request.Body);
        _db.LessonQuestions.Add(q);
        await _db.SaveChangesAsync(ct);

        // Notify the course teacher there's a new question
        var teacherId = await _db.Lessons
            .Where(l => l.Id == request.LessonId)
            .Select(l => (Guid?)l.Chapter.Course.TeacherId)
            .FirstOrDefaultAsync(ct);
        if (teacherId is Guid tid)
        {
            await _notifications.SendAsync(tid, "lesson.question",
                "❓ سؤال جديد على درسك",
                $"{name} سأل: {Trim(request.Body, 80)}",
                new { questionId = q.Id, url = $"/teacher/lessons/{request.LessonId}/qa" }, ct);
        }

        return Result.Success(q.Id);
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…";
}

// ─── Answer a question (teacher / admin) ───────────────
public sealed record AnswerLessonQuestionCommand(Guid QuestionId, string Body) : ICommand;

public sealed class AnswerLessonQuestionValidator : AbstractValidator<AnswerLessonQuestionCommand>
{
    public AnswerLessonQuestionValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MinimumLength(2).MaximumLength(4000);
    }
}

public sealed class AnswerLessonQuestionHandler : ICommandHandler<AnswerLessonQuestionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public AnswerLessonQuestionHandler(IApplicationDbContext db, ICurrentUser currentUser, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications;
    }

    public async Task<Result> Handle(AnswerLessonQuestionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var question = await _db.LessonQuestions
            .Include(q => q.Lesson).ThenInclude(l => l.Chapter).ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, ct);
        if (question is null)
            return Error.NotFound("QUESTION.NOT_FOUND", "السؤال غير موجود", "Not found");

        var isAdmin = _currentUser.IsInRole("Admin");
        var isCourseTeacher = question.Lesson.Chapter.Course.TeacherId == userId;
        if (!isAdmin && !isCourseTeacher)
            return Error.Forbidden("ANSWER.FORBIDDEN", "فقط المدرّس أو الأدمن يردّ", "Teacher only");

        var name = await _db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(ct)
                   ?? "المدرّس";

        question.Answer(userId, name, request.Body);
        await _db.SaveChangesAsync(ct);

        // Notify the asker
        await _notifications.SendAsync(question.AskedByUserId, "lesson.answer",
            "💬 رد على سؤالك",
            $"رد {name} على سؤالك في الدرس.",
            new { questionId = question.Id, url = $"/lessons/{question.LessonId}/watch" }, ct);

        return Result.Success();
    }
}

// ─── Get questions for a lesson ────────────────────────
public sealed record GetLessonQuestionsQuery(Guid LessonId, int Take = 50) : IQuery<LessonQuestionsResponse>;

public sealed class GetLessonQuestionsHandler : IQueryHandler<GetLessonQuestionsQuery, LessonQuestionsResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetLessonQuestionsHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<LessonQuestionsResponse>> Handle(GetLessonQuestionsQuery request, CancellationToken ct)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);
        if (lesson is null)
            return Error.NotFound("LESSON.NOT_FOUND", "الدرس غير موجود", "Not found");

        bool canAsk = false, canAnswer = false;
        if (_currentUser.UserId is Guid uid)
        {
            canAsk = await _access.CanAccessLessonAsync(uid, request.LessonId, ct);
            canAnswer = _currentUser.IsInRole("Admin") || lesson.Chapter.Course.TeacherId == uid;
        }

        var take = Math.Clamp(request.Take, 1, 200);
        var rawQs = await _db.LessonQuestions
            .Where(q => q.LessonId == request.LessonId)
            .Select(q => new
            {
                q.Id, q.LessonId, q.AskedByUserId, q.AskedByName,
                q.Body, q.CreatedAtUtc,
                q.AnswerBody, q.AnsweredByName, q.AnsweredAtUtc,
                VotesCount = _db.LessonQuestionVotes.Count(v => v.QuestionId == q.Id)
            })
            .ToListAsync(ct);

        var myUid = _currentUser.UserId;
        HashSet<Guid> myVotes = new();
        if (myUid is Guid u)
        {
            var ids = rawQs.Select(q => q.Id).ToList();
            myVotes = (await _db.LessonQuestionVotes
                .Where(v => v.StudentId == u && ids.Contains(v.QuestionId))
                .Select(v => v.QuestionId)
                .ToListAsync(ct)).ToHashSet();
        }

        // Order: unanswered + high-vote first, then newest. This puts the
        // questions the teacher most needs to address at the top.
        var qs = rawQs
            .OrderByDescending(q => q.VotesCount)
            .ThenByDescending(q => q.AnswerBody == null)
            .ThenByDescending(q => q.CreatedAtUtc)
            .Take(take)
            .Select(q => new LessonQuestionDto(
                q.Id, q.LessonId,
                q.AskedByUserId, q.AskedByName,
                q.Body, q.CreatedAtUtc,
                q.AnswerBody, q.AnsweredByName, q.AnsweredAtUtc,
                q.VotesCount,
                myVotes.Contains(q.Id)))
            .ToList();

        var total = rawQs.Count;
        var unanswered = rawQs.Count(q => q.AnswerBody == null);

        return Result.Success(new LessonQuestionsResponse(total, unanswered, canAsk, canAnswer, qs));
    }
}

// ─── Toggle a vote on a question ─────────────────────
public sealed record ToggleQuestionVoteCommand(Guid QuestionId) : ICommand<VoteQuestionResponse>;

public sealed class ToggleQuestionVoteHandler
    : ICommandHandler<ToggleQuestionVoteCommand, VoteQuestionResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public ToggleQuestionVoteHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<VoteQuestionResponse>> Handle(
        ToggleQuestionVoteCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var isStudent = await _db.Students.AnyAsync(s => s.Id == userId, ct);
        if (!isStudent)
            return Error.Forbidden("VOTE.STUDENTS_ONLY", "التصويت للطلاب فقط", "Students only");

        var question = await _db.LessonQuestions
            .Include(q => q.Lesson).ThenInclude(l => l.Chapter)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, ct);
        if (question is null)
            return Error.NotFound("QUESTION.NOT_FOUND", "السؤال غير موجود", "Not found");

        // Author cannot vote on their own question (avoids self-boost).
        if (question.AskedByUserId == userId)
            return Error.Forbidden("VOTE.SELF", "لا تستطيع التصويت على سؤالك", "Self-vote");

        var hasAccess = await _access.CanAccessLessonAsync(userId, question.LessonId, ct);
        if (!hasAccess)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية الوصول لهذا الدرس", "No access");

        var existing = await _db.LessonQuestionVotes
            .FirstOrDefaultAsync(v => v.StudentId == userId && v.QuestionId == request.QuestionId, ct);

        bool nowActive;
        if (existing is null)
        {
            _db.LessonQuestionVotes.Add(new LessonQuestionVote(userId, request.QuestionId));
            nowActive = true;
        }
        else
        {
            _db.LessonQuestionVotes.Remove(existing);
            nowActive = false;
        }

        await _db.SaveChangesAsync(ct);

        var count = await _db.LessonQuestionVotes.CountAsync(v => v.QuestionId == request.QuestionId, ct);
        return Result.Success(new VoteQuestionResponse(nowActive, count));
    }
}
