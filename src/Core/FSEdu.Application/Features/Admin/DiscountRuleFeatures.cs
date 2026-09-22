using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Shared.Contracts.Admin;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin;

// ─── List ─────────────────────────────────────────────
public sealed record GetDiscountRulesQuery() : IQuery<List<DiscountRuleDto>>;

public sealed class GetDiscountRulesHandler : IQueryHandler<GetDiscountRulesQuery, List<DiscountRuleDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetDiscountRulesHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<List<DiscountRuleDto>>> Handle(GetDiscountRulesQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("RULES.FORBIDDEN", "للأدمن فقط", "Admin only");

        var list = await _db.DiscountRules
            .OrderByDescending(r => r.Active).ThenBy(r => r.Type)
            .Select(r => new DiscountRuleDto(
                r.Id, r.Code, r.TitleAr, r.DescriptionAr,
                r.Type.ToString(), r.DiscountPct, r.ThresholdInt,
                r.Active, r.ValidFromUtc, r.ValidUntilUtc))
            .ToListAsync(ct);
        return Result.Success(list);
    }
}

// ─── Create ───────────────────────────────────────────
public sealed record CreateDiscountRuleCommand(
    string Code, string TitleAr, string? DescriptionAr,
    string Type, decimal DiscountPct, int? ThresholdInt,
    DateTime? ValidFromUtc, DateTime? ValidUntilUtc
) : ICommand<int>;

public sealed class CreateDiscountRuleValidator : AbstractValidator<CreateDiscountRuleCommand>
{
    public CreateDiscountRuleValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches("^[a-z0-9_-]+$").WithMessage("الكود حروف إنجليزية صغيرة وأرقام وشُرَط فقط");
        RuleFor(x => x.TitleAr).NotEmpty().MinimumLength(3).MaximumLength(255);
        RuleFor(x => x.DescriptionAr).MaximumLength(500);
        RuleFor(x => x.Type).Must(t => Enum.TryParse<DiscountRuleType>(t, out _))
            .WithMessage("نوع الخصم غير معروف");
        RuleFor(x => x.DiscountPct).GreaterThan(0m).LessThanOrEqualTo(100m);
        When(x => x.ThresholdInt.HasValue, () =>
            RuleFor(x => x.ThresholdInt!.Value).GreaterThan(0));
    }
}

public sealed class CreateDiscountRuleHandler : ICommandHandler<CreateDiscountRuleCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public CreateDiscountRuleHandler(IApplicationDbContext db, ICurrentUser currentUser, IAuditLog audit)
    { _db = db; _currentUser = currentUser; _audit = audit; }

    public async Task<Result<int>> Handle(CreateDiscountRuleCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("RULES.FORBIDDEN", "للأدمن فقط", "Admin only");

        var code = request.Code.Trim().ToLowerInvariant();
        var exists = await _db.DiscountRules.AnyAsync(r => r.Code == code, ct);
        if (exists)
            return Error.Conflict("RULE.DUPLICATE", "كود القاعدة مستخدم مسبقًا", "Code exists");

        var type = Enum.Parse<DiscountRuleType>(request.Type);
        var rule = new DiscountRule(
            code, request.TitleAr, request.DescriptionAr,
            type, request.DiscountPct, request.ThresholdInt,
            request.ValidFromUtc, request.ValidUntilUtc);
        _db.DiscountRules.Add(rule);
        await _db.SaveChangesAsync(ct);

        await _audit.RecordAsync("discount-rule.create", "DiscountRule", rule.Id.ToString(),
            new { code, request.Type, request.DiscountPct, request.ThresholdInt }, ct);

        return Result.Success(rule.Id);
    }
}

// ─── Toggle Active ────────────────────────────────────
public sealed record SetDiscountRuleActiveCommand(int RuleId, bool Active) : ICommand;

public sealed class SetDiscountRuleActiveHandler : ICommandHandler<SetDiscountRuleActiveCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public SetDiscountRuleActiveHandler(IApplicationDbContext db, ICurrentUser currentUser, IAuditLog audit)
    { _db = db; _currentUser = currentUser; _audit = audit; }

    public async Task<Result> Handle(SetDiscountRuleActiveCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("RULES.FORBIDDEN", "للأدمن فقط", "Admin only");

        var rule = await _db.DiscountRules.FirstOrDefaultAsync(r => r.Id == request.RuleId, ct);
        if (rule is null)
            return Error.NotFound("RULE.NOT_FOUND", "القاعدة غير موجودة", "Not found");

        if (request.Active) rule.Reactivate(); else rule.Deactivate();
        await _db.SaveChangesAsync(ct);

        await _audit.RecordAsync(request.Active ? "discount-rule.activate" : "discount-rule.deactivate",
            "DiscountRule", rule.Id.ToString(), new { code = rule.Code }, ct);

        return Result.Success();
    }
}
