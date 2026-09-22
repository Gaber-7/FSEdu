using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// ─── Submit / update a review (idempotent) ────────────
public sealed record SubmitCourseReviewCommand(Guid CourseId, int Rating, string? Comment) : ICommand;

public sealed class SubmitCourseReviewValidator : AbstractValidator<SubmitCourseReviewCommand>
{
    public SubmitCourseReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(1500);
    }
}

public sealed class SubmitCourseReviewHandler : ICommandHandler<SubmitCourseReviewCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public SubmitCourseReviewHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result> Handle(SubmitCourseReviewCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var isStudent = await _db.Students.AnyAsync(s => s.Id == studentId, ct);
        if (!isStudent)
            return Error.Forbidden("STUDENT.ONLY", "للطلاب فقط", "Students only");

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        var hasAccess = await _access.CanAccessSubjectAsync(studentId, course.SubjectId, ct);
        if (!hasAccess)
            return Error.Forbidden("ACCESS.DENIED", "يجب أن تكون مشتركًا في المادة لتقييم الدورة", "Subscription required");

        var existing = await _db.CourseReviews
            .FirstOrDefaultAsync(r => r.CourseId == request.CourseId && r.StudentId == studentId, ct);

        if (existing is null)
            _db.CourseReviews.Add(new CourseReview(studentId, request.CourseId, request.Rating, request.Comment));
        else
            existing.Update(request.Rating, request.Comment);

        await _db.SaveChangesAsync(ct);

        // Recompute course aggregate from authoritative table
        var stats = await _db.CourseReviews
            .Where(r => r.CourseId == request.CourseId)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Avg = g.Average(r => (decimal)r.Rating) })
            .FirstOrDefaultAsync(ct);
        course.SetRatingStats(stats?.Count ?? 0, stats?.Avg ?? 0m);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

// ─── Get reviews for a course ─────────────────────────
public sealed record GetCourseReviewsQuery(Guid CourseId, int Take = 50) : IQuery<CourseReviewsResponse>;

public sealed class GetCourseReviewsHandler : IQueryHandler<GetCourseReviewsQuery, CourseReviewsResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetCourseReviewsHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<CourseReviewsResponse>> Handle(GetCourseReviewsQuery request, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        var reviews = await _db.CourseReviews
            .Where(r => r.CourseId == request.CourseId)
            .OrderByDescending(r => r.UpdatedAtUtc ?? r.CreatedAtUtc)
            .Take(Math.Clamp(request.Take, 1, 200))
            .Select(r => new CourseReviewDto(
                r.StudentId, r.Student.FullName, r.Rating, r.Comment,
                r.CreatedAtUtc, r.UpdatedAtUtc))
            .ToListAsync(ct);

        var stars = new int[5];
        var allRatings = await _db.CourseReviews
            .Where(r => r.CourseId == request.CourseId)
            .Select(r => r.Rating)
            .ToListAsync(ct);
        foreach (var r in allRatings)
        {
            if (r >= 1 && r <= 5) stars[r - 1]++;
        }

        CourseReviewDto? myReview = null;
        bool canReview = false;
        if (_currentUser.UserId is Guid uid)
        {
            myReview = reviews.FirstOrDefault(r => r.StudentId == uid);
            if (myReview is null)
            {
                myReview = await _db.CourseReviews
                    .Where(r => r.CourseId == request.CourseId && r.StudentId == uid)
                    .Select(r => new CourseReviewDto(
                        r.StudentId, r.Student.FullName, r.Rating, r.Comment,
                        r.CreatedAtUtc, r.UpdatedAtUtc))
                    .FirstOrDefaultAsync(ct);
            }

            var isStudent = await _db.Students.AnyAsync(s => s.Id == uid, ct);
            canReview = isStudent && await _access.CanAccessSubjectAsync(uid, course.SubjectId, ct);
        }

        return Result.Success(new CourseReviewsResponse(
            course.RatingAvg, course.RatingsCount,
            stars, myReview, canReview, reviews));
    }
}
