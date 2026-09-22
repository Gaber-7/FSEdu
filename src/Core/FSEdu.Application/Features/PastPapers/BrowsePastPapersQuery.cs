using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Assessments;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.PastPapers;

public sealed record BrowsePastPapersQuery(
    int? SubjectId,
    int? StageId,
    int? Year,
    string? Term,
    string? ExamType
) : IQuery<List<PastPaperListItemDto>>;

public sealed class BrowsePastPapersHandler : IQueryHandler<BrowsePastPapersQuery, List<PastPaperListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public BrowsePastPapersHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<List<PastPaperListItemDto>>> Handle(BrowsePastPapersQuery request, CancellationToken ct)
    {
        var studentId = _currentUser.UserId ?? Guid.Empty;

        var q = _db.PastPapers.Where(p => p.Published);
        if (request.SubjectId.HasValue) q = q.Where(p => p.SubjectId == request.SubjectId.Value);
        if (request.StageId.HasValue) q = q.Where(p => p.StageId == request.StageId.Value);
        if (request.Year.HasValue) q = q.Where(p => p.Year == request.Year.Value);
        if (!string.IsNullOrEmpty(request.Term) && Enum.TryParse<FSEdu.Domain.Assessments.PastPaperTerm>(request.Term, out var t))
            q = q.Where(p => p.Term == t);
        if (!string.IsNullOrEmpty(request.ExamType) && Enum.TryParse<FSEdu.Domain.Assessments.PastPaperExamType>(request.ExamType, out var et))
            q = q.Where(p => p.ExamType == et);

        var rows = await q
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.SubjectId)
            .Select(p => new
            {
                p.Id, p.Title, p.Year, p.Term, p.ExamType,
                SubjectName = p.Subject.NameAr,
                SubjectColor = p.Subject.ColorHex,
                StageName = p.Stage.NameAr,
                p.EducationalAdministration,
                p.DurationMinutes,
                p.TotalMarks,
                QuestionCount = p.Questions.Count,
            })
            .ToListAsync(ct);

        // Best score per paper for this student
        var paperIds = rows.Select(r => r.Id).ToList();
        var bestScores = await _db.PastPaperAttempts
            .Where(a => a.StudentId == studentId && paperIds.Contains(a.PastPaperId) && a.SubmittedAtUtc != null)
            .GroupBy(a => a.PastPaperId)
            .Select(g => new { PaperId = g.Key, Best = g.Max(a => a.Score) })
            .ToDictionaryAsync(x => x.PaperId, x => (decimal?)x.Best, ct);

        return Result.Success(rows.Select(r => new PastPaperListItemDto(
            r.Id, r.Title, r.Year, r.Term.ToString(), r.ExamType.ToString(),
            r.SubjectName, r.SubjectColor, r.StageName, r.EducationalAdministration,
            r.DurationMinutes, r.TotalMarks, r.QuestionCount,
            bestScores.TryGetValue(r.Id, out var s) ? s : null
        )).ToList());
    }
}

// ─── Filter options ─────────────────────────────
public sealed record GetPastPaperFiltersQuery() : IQuery<PastPaperBrowseFiltersDto>;

public sealed class GetPastPaperFiltersHandler : IQueryHandler<GetPastPaperFiltersQuery, PastPaperBrowseFiltersDto>
{
    private readonly IApplicationDbContext _db;
    public GetPastPaperFiltersHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PastPaperBrowseFiltersDto>> Handle(GetPastPaperFiltersQuery request, CancellationToken ct)
    {
        var subjectIds = await _db.PastPapers.Where(p => p.Published).Select(p => p.SubjectId).Distinct().ToListAsync(ct);
        var subjects = await _db.Subjects.Where(s => subjectIds.Contains(s.Id))
            .Select(s => new PastPaperFilterOption(s.Id, s.NameAr)).ToListAsync(ct);

        var stageIds = await _db.PastPapers.Where(p => p.Published).Select(p => p.StageId).Distinct().ToListAsync(ct);
        var stages = await _db.Stages.Where(s => stageIds.Contains(s.Id))
            .Select(s => new PastPaperFilterOption(s.Id, s.NameAr)).ToListAsync(ct);

        var years = await _db.PastPapers.Where(p => p.Published)
            .Select(p => p.Year).Distinct().OrderByDescending(y => y).ToArrayAsync(ct);

        var terms = Enum.GetNames(typeof(FSEdu.Domain.Assessments.PastPaperTerm));
        var examTypes = Enum.GetNames(typeof(FSEdu.Domain.Assessments.PastPaperExamType));

        return Result.Success(new PastPaperBrowseFiltersDto(
            subjects.ToArray(), stages.ToArray(), years, terms, examTypes));
    }
}
