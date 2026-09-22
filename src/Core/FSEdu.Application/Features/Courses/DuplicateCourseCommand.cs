using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// Clones an existing course (owned by the current teacher) — including its
// chapters, lessons, and lesson attachments — into a new Draft course.
// Does NOT copy: enrollments, lesson progress, reviews, questions, notes,
// bookmarks, announcements, markers, ratings, or live sessions. Status is
// reset to Draft so the teacher can review before submitting for approval.
public sealed record DuplicateCourseCommand(Guid SourceCourseId, string? NewTitle = null)
    : ICommand<Guid>;

public sealed class DuplicateCourseValidator : AbstractValidator<DuplicateCourseCommand>
{
    public DuplicateCourseValidator()
    {
        RuleFor(x => x.NewTitle).MaximumLength(255);
    }
}

public sealed class DuplicateCourseHandler : ICommandHandler<DuplicateCourseCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DuplicateCourseHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(DuplicateCourseCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var src = await _db.Courses
            .Include(c => c.Chapters)
                .ThenInclude(ch => ch.Lessons)
                    .ThenInclude(l => l.Attachments)
            .FirstOrDefaultAsync(c => c.Id == request.SourceCourseId, ct);

        if (src is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        var isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin && src.TeacherId != userId)
            return Error.Forbidden("COURSE.FORBIDDEN", "لا يمكنك تكرار دورة ليست لك", "Not your course");

        var newTitle = string.IsNullOrWhiteSpace(request.NewTitle)
            ? $"{src.Title} (نسخة)"
            : request.NewTitle.Trim();

        var clone = new Course(Guid.NewGuid(), src.TeacherId, src.SubjectId, src.StageId,
            newTitle, src.Price, src.Term);
        clone.UpdateDetails(newTitle, src.Description, src.ThumbnailUrl, src.Price);
        // Status remains Draft by default — the teacher must SubmitForReview manually.

        _db.Courses.Add(clone);

        // Chapters + Lessons + Attachments
        foreach (var srcChapter in src.Chapters.OrderBy(c => c.OrderNum))
        {
            var newChapter = new Chapter(Guid.NewGuid(), clone.Id, srcChapter.Title, srcChapter.OrderNum);

            foreach (var srcLesson in srcChapter.Lessons.OrderBy(l => l.OrderNum))
            {
                var newLesson = new Lesson(Guid.NewGuid(), newChapter.Id,
                    srcLesson.Title, srcLesson.Type, srcLesson.OrderNum);

                // Re-attach video reference (same URL — teacher can replace later)
                if (!string.IsNullOrEmpty(srcLesson.VideoUrl))
                    newLesson.SetVideo(srcLesson.VideoUrl, srcLesson.VideoDrmKeyId, srcLesson.DurationSeconds);

                // Preview flag carries over
                if (srcLesson.IsFreePreview) newLesson.MarkFreePreview();

                // Attachments — clone metadata, keep same file URLs (no file copy)
                foreach (var att in srcLesson.Attachments)
                {
                    newLesson.AddAttachment(new LessonAttachment(
                        Guid.NewGuid(), newLesson.Id,
                        att.Title, att.FileUrl, att.FileType, att.SizeBytes,
                        att.DownloadAllowed));
                }

                newChapter.AddLesson(newLesson);
            }

            // Domain doesn't expose a public "AddExistingChapter" — use a direct
            // collection add via the navigation property tracked by EF.
            _db.Chapters.Add(newChapter);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success(clone.Id);
    }
}
