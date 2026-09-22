using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

public sealed record GetMyCertificatesQuery() : IQuery<List<MyCertificateRowDto>>;

public sealed class GetMyCertificatesHandler : IQueryHandler<GetMyCertificatesQuery, List<MyCertificateRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyCertificatesHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<MyCertificateRowDto>>> Handle(GetMyCertificatesQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;

        var rows = await (
            from cert in _db.Certificates
            where cert.StudentId == studentId
            join course in _db.Courses on cert.CourseId equals course.Id
            join subject in _db.Subjects on course.SubjectId equals subject.Id
            orderby cert.IssuedAtUtc descending
            select new MyCertificateRowDto(
                cert.CourseId,
                course.Title,
                subject.NameAr,
                subject.ColorHex,
                course.ThumbnailUrl,
                cert.CertificateNumber,
                cert.IssuedAtUtc,
                cert.FinalGrade
            )).ToListAsync(ct);

        return Result.Success(rows);
    }
}
