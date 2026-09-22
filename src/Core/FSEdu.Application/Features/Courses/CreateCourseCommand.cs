using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

public sealed record CreateCourseCommand(
    int SubjectId,
    int StageId,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    string Term,
    string? PreviewVideoUrl = null
) : ICommand<Guid>;

public sealed class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(5).MaximumLength(255);
        RuleFor(x => x.SubjectId).GreaterThan(0);
        RuleFor(x => x.StageId).GreaterThan(0);
        RuleFor(x => x.Term).Must(t => t is "First" or "Second" or "Annual");
    }
}

public sealed class CreateCourseHandler : ICommandHandler<CreateCourseCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateCourseHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Authentication required");

        var teacherId = _currentUser.UserId.Value;

        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId, ct);
        if (teacher is null)
            return Error.Forbidden("AUTH.NOT_TEACHER", "هذا الحساب ليس مدرسًا", "Not a teacher account");

        if (!teacher.Verified)
            return Error.Forbidden("TEACHER.NOT_VERIFIED",
                "حسابك لم يُعتمد بعد من الإدارة", "Teacher account not verified");

        var subjectExists = await _db.Subjects.AnyAsync(s => s.Id == request.SubjectId, ct);
        if (!subjectExists)
            return Error.NotFound("SUBJECT.NOT_FOUND", "المادة غير موجودة", "Subject not found");

        var stageExists = await _db.Stages.AnyAsync(s => s.Id == request.StageId, ct);
        if (!stageExists)
            return Error.NotFound("STAGE.NOT_FOUND", "المرحلة غير موجودة", "Stage not found");

        var term = Enum.Parse<AcademicTerm>(request.Term);

        var course = new Course(Guid.NewGuid(), teacherId, request.SubjectId, request.StageId,
            request.Title, 0m, term);
        course.UpdateDetails(request.Title, request.Description, request.ThumbnailUrl, 0m);
        course.SetPreviewVideo(request.PreviewVideoUrl);

        _db.Courses.Add(course);
        await _db.SaveChangesAsync(ct);

        return Result.Success(course.Id);
    }
}

// ─── Add Chapter ─────────────────────────────
public sealed record AddChapterCommand(Guid CourseId, string Title, int OrderNum) : ICommand<Guid>;

public sealed class AddChapterHandler : ICommandHandler<AddChapterCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AddChapterHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(AddChapterCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId && c.TeacherId == teacherId, ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Course not found");

        // Server-side order calculation (ignores client value to prevent race conditions)
        var maxOrder = await _db.Chapters
            .Where(c => c.CourseId == course.Id)
            .Select(c => (int?)c.OrderNum)
            .MaxAsync(ct) ?? 0;

        var chapter = new Chapter(Guid.NewGuid(), course.Id, request.Title, maxOrder + 1);
        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync(ct);

        return Result.Success(chapter.Id);
    }
}

// ─── Delete Chapter ──────────────────────────
public sealed record DeleteChapterCommand(Guid ChapterId) : ICommand;

public sealed class DeleteChapterHandler : ICommandHandler<DeleteChapterCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteChapterHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result> Handle(DeleteChapterCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var chapter = await _db.Chapters
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == request.ChapterId, ct);

        if (chapter is null)
            return Error.NotFound("CHAPTER.NOT_FOUND", "الفصل غير موجود", "Chapter not found");
        if (chapter.Course.TeacherId != teacherId)
            return Error.Forbidden("COURSE.NOT_OWNER", "ليس فصلك", "Not your chapter");

        _db.Chapters.Remove(chapter);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Add Lesson ─────────────────────────────
public sealed record AddLessonCommand(
    Guid ChapterId,
    string Title,
    string Type,
    int DurationSeconds,
    string? VideoUrl,
    DateTime? ScheduledAtUtc,
    bool IsFreePreview,
    int OrderNum
) : ICommand<Guid>;

public sealed class AddLessonHandler : ICommandHandler<AddLessonCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AddLessonHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(AddLessonCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var chapter = await _db.Chapters
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == request.ChapterId, ct);
        if (chapter is null)
            return Error.NotFound("CHAPTER.NOT_FOUND", "الفصل غير موجود", "Chapter not found");
        if (chapter.Course.TeacherId != teacherId)
            return Error.Forbidden("COURSE.NOT_OWNER", "ليست دورتك", "Not your course");

        // Server-side order calculation
        var maxOrder = await _db.Lessons
            .Where(l => l.ChapterId == chapter.Id)
            .Select(l => (int?)l.OrderNum)
            .MaxAsync(ct) ?? 0;

        var type = Enum.Parse<LessonType>(request.Type);
        var lesson = new Lesson(Guid.NewGuid(), chapter.Id, request.Title, type, maxOrder + 1);

        if (!string.IsNullOrWhiteSpace(request.VideoUrl))
            lesson.SetVideo(request.VideoUrl, null, request.DurationSeconds);

        if (request.ScheduledAtUtc.HasValue) lesson.Schedule(request.ScheduledAtUtc.Value);
        if (request.IsFreePreview) lesson.MarkFreePreview();

        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync(ct);

        return Result.Success(lesson.Id);
    }
}

// ─── Delete Lesson ───────────────────────────
public sealed record DeleteLessonCommand(Guid LessonId) : ICommand;

public sealed class DeleteLessonHandler : ICommandHandler<DeleteLessonCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteLessonHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result> Handle(DeleteLessonCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var lesson = await _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);

        if (lesson is null)
            return Error.NotFound("LESSON.NOT_FOUND", "الدرس غير موجود", "Lesson not found");
        if (lesson.Chapter.Course.TeacherId != teacherId)
            return Error.Forbidden("COURSE.NOT_OWNER", "ليس درسك", "Not your lesson");

        _db.Lessons.Remove(lesson);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Submit Course for Review ─────────────────
public sealed record SubmitCourseForReviewCommand(Guid CourseId) : ICommand;

public sealed class SubmitCourseForReviewHandler : ICommandHandler<SubmitCourseForReviewCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SubmitCourseForReviewHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(SubmitCourseForReviewCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var course = await _db.Courses
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.TeacherId == teacherId, ct);

        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Course not found");

        if (!course.Chapters.Any() || !course.Chapters.Any(ch => ch.Lessons.Any()))
            return Error.Validation("COURSE.EMPTY",
                "الدورة فارغة — أضف فصلًا ودرسًا واحدًا على الأقل قبل الإرسال",
                "Course must have at least one chapter with one lesson");

        course.SubmitForReview();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
