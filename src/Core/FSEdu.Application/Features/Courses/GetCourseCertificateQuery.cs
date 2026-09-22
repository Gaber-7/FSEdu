using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

public sealed record GetCourseCertificateQuery(Guid CourseId) : IQuery<CourseCertificateDto>;

public sealed class GetCourseCertificateHandler : IQueryHandler<GetCourseCertificateQuery, CourseCertificateDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetCourseCertificateHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<CourseCertificateDto>> Handle(GetCourseCertificateQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
            return Error.Forbidden("STUDENT.ONLY", "للطلاب فقط", "Students only");

        var course = await _db.Courses
            .Where(c => c.Id == request.CourseId)
            .Include(c => c.Subject)
            .Include(c => c.Stage)
            .Include(c => c.Teacher)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .FirstOrDefaultAsync(ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Course not found");

        // Access check
        var hasAccess = await _access.CanAccessSubjectAsync(studentId, course.SubjectId, ct);
        if (!hasAccess)
            return Error.Forbidden("ACCESS.DENIED", "غير مشترك في هذه المادة", "No subscription");

        var lessonIds = course.Chapters.SelectMany(ch => ch.Lessons).Select(l => l.Id).ToList();
        if (lessonIds.Count == 0)
            return Error.Validation("CERT.NO_LESSONS", "لا توجد دروس في هذه الدورة بعد", "No lessons");

        var progress = await _db.LessonProgress
            .Where(lp => lp.StudentId == studentId && lessonIds.Contains(lp.LessonId))
            .ToListAsync(ct);
        var completed = progress.Where(p => p.Completed).ToList();
        if (completed.Count < lessonIds.Count)
            return Error.Validation("CERT.NOT_COMPLETED",
                $"لم تكمل الدورة بعد ({completed.Count} من {lessonIds.Count} درس). أكمل كل الدروس للحصول على الشهادة.",
                "Course not completed");

        var avgWatched = progress.Average(p => p.WatchedPct);

        // Persist (or fetch) the certificate so issue date + number are stable
        var cert = await _db.Certificates
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == course.Id, ct);
        if (cert is null)
        {
            cert = new Certificate(Guid.NewGuid(), studentId, course.Id, avgWatched);
            _db.Certificates.Add(cert);
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success(new CourseCertificateDto(
            course.Id, course.Title,
            course.Subject.NameAr, course.Stage.NameAr,
            course.Teacher.FullName,
            student.FullName,
            lessonIds.Count,
            cert.IssuedAtUtc,
            cert.CertificateNumber));
    }
}
