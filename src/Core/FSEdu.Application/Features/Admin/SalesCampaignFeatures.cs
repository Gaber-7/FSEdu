using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Shared.Contracts.Admin;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin;

// ─── List campaigns ────────────────────────────────────
public sealed record GetCampaignsListQuery() : IQuery<List<SalesCampaignDto>>;

public sealed class GetCampaignsListHandler : IQueryHandler<GetCampaignsListQuery, List<SalesCampaignDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCampaignsListHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<SalesCampaignDto>>> Handle(GetCampaignsListQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("CAMPAIGNS.FORBIDDEN", "للأدمن فقط", "Admin only");

        var now = DateTime.UtcNow;
        var rows = await _db.SalesCampaigns
            .OrderByDescending(c => c.Active)
            .ThenByDescending(c => c.ValidFromUtc)
            .ToListAsync(ct);

        // Resolve scope names in batches to avoid N+1.
        var stageIds = rows.Where(r => r.Scope == CampaignScope.Stage && r.ScopeId.HasValue)
            .Select(r => r.ScopeId!.Value).Distinct().ToList();
        var subjectIds = rows.Where(r => r.Scope == CampaignScope.Subject && r.ScopeId.HasValue)
            .Select(r => r.ScopeId!.Value).Distinct().ToList();

        var stageNames = await _db.Stages
            .Where(s => stageIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.NameAr, ct);
        var subjectNames = await _db.Subjects
            .Where(s => subjectIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.NameAr, ct);

        string? Resolve(SalesCampaign c) => c.Scope switch
        {
            CampaignScope.All => null,
            CampaignScope.Stage when c.ScopeId.HasValue && stageNames.TryGetValue(c.ScopeId.Value, out var n) => n,
            CampaignScope.Subject when c.ScopeId.HasValue && subjectNames.TryGetValue(c.ScopeId.Value, out var n) => n,
            _ => null
        };

        var list = rows.Select(c => new SalesCampaignDto(
            c.Id, c.Title, c.DiscountPct,
            c.Scope.ToString(), c.ScopeId, Resolve(c),
            c.ValidFromUtc, c.ValidUntilUtc, c.Active,
            c.Active && c.ValidFromUtc <= now && c.ValidUntilUtc >= now
        )).ToList();

        return Result.Success(list);
    }
}

// ─── Create ──────────────────────────────────────────
public sealed record CreateCampaignCommand(
    string Title, decimal DiscountPct, string Scope, int? ScopeId,
    DateTime ValidFromUtc, DateTime ValidUntilUtc
) : ICommand<int>;

public sealed class CreateCampaignValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(255);
        RuleFor(x => x.DiscountPct).GreaterThan(0m).LessThanOrEqualTo(100m);
        RuleFor(x => x.Scope).Must(s => s is "All" or "Stage" or "Subject");
        RuleFor(x => x.ValidUntilUtc).GreaterThan(x => x.ValidFromUtc)
            .WithMessage("تاريخ الانتهاء يجب أن يكون بعد البداية");
        When(x => x.Scope is "Stage" or "Subject", () =>
            RuleFor(x => x.ScopeId).NotNull().GreaterThan(0)
                .WithMessage("اختر المرحلة/المادة المستهدفة"));
    }
}

public sealed class CreateCampaignHandler : ICommandHandler<CreateCampaignCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public CreateCampaignHandler(IApplicationDbContext db, ICurrentUser currentUser, IAuditLog audit)
    {
        _db = db; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<int>> Handle(CreateCampaignCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("CAMPAIGNS.FORBIDDEN", "للأدمن فقط", "Admin only");

        var scope = Enum.Parse<CampaignScope>(request.Scope);
        var campaign = new SalesCampaign(
            request.Title, request.DiscountPct, scope, request.ScopeId,
            request.ValidFromUtc, request.ValidUntilUtc);
        _db.SalesCampaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);

        await _audit.RecordAsync("campaign.create", "SalesCampaign", campaign.Id.ToString(),
            new { request.Title, request.DiscountPct, request.Scope, request.ScopeId,
                  request.ValidFromUtc, request.ValidUntilUtc }, ct);

        return Result.Success(campaign.Id);
    }
}

// ─── Toggle Active ───────────────────────────────────
public sealed record SetCampaignActiveCommand(int CampaignId, bool Active) : ICommand;

public sealed class SetCampaignActiveHandler : ICommandHandler<SetCampaignActiveCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SetCampaignActiveHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(SetCampaignActiveCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("CAMPAIGNS.FORBIDDEN", "للأدمن فقط", "Admin only");

        var c = await _db.SalesCampaigns.FirstOrDefaultAsync(x => x.Id == request.CampaignId, ct);
        if (c is null)
            return Error.NotFound("CAMPAIGN.NOT_FOUND", "الحملة غير موجودة", "Not found");

        if (request.Active) c.Reactivate(); else c.Deactivate();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
