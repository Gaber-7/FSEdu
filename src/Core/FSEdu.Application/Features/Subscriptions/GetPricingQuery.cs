using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Shared.Contracts.Subscriptions;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Subscriptions;

public sealed record GetPricingQuery() : IQuery<PricingResponse>;

public sealed class GetPricingHandler : IQueryHandler<GetPricingQuery, PricingResponse>
{
    private readonly IApplicationDbContext _db;
    public GetPricingHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PricingResponse>> Handle(GetPricingQuery request, CancellationToken ct)
    {
        var stages = await _db.Stages
            .Where(s => s.ParentStageId != null)
            .Include(s => s.Subjects.Where(sb => sb.IsActive))
            .OrderBy(s => s.LevelOrder)
            .ToListAsync(ct);

        // ─── Load active campaigns once ─────────────────
        var now = DateTime.UtcNow;
        var campaigns = await _db.SalesCampaigns
            .Where(c => c.Active && c.ValidFromUtc <= now && c.ValidUntilUtc >= now)
            .ToListAsync(ct);

        // Resolve scope names for the banner
        var stageNames = stages.ToDictionary(s => s.Id, s => s.NameAr);
        var allSubjects = stages.SelectMany(s => s.Subjects)
            .GroupBy(s => s.Id).Select(g => g.First())
            .ToDictionary(s => s.Id, s => s.NameAr);

        // Stage discount: best campaign that targets the stage OR All
        decimal BestForStage(int stageId)
        {
            decimal best = 0;
            foreach (var c in campaigns)
            {
                if (c.Scope == CampaignScope.All && c.DiscountPct > best) best = c.DiscountPct;
                else if (c.Scope == CampaignScope.Stage && c.ScopeId == stageId && c.DiscountPct > best) best = c.DiscountPct;
            }
            return best;
        }

        // Subject discount: best campaign that targets the subject OR the parent stage OR All
        decimal BestForSubject(int subjectId, int stageId)
        {
            decimal best = BestForStage(stageId);
            foreach (var c in campaigns)
            {
                if (c.Scope == CampaignScope.Subject && c.ScopeId == subjectId && c.DiscountPct > best)
                    best = c.DiscountPct;
            }
            return best;
        }

        var stageDtos = stages.Select(s => new StagePricingDto(
            s.Id, s.NameAr, s.FullTermPriceEgp,
            BestForStage(s.Id),
            s.Subjects.Select(sub => new SubjectPricingDto(
                sub.Id, sub.NameAr, sub.ColorHex,
                sub.MonthlyPriceEgp, sub.TermPriceEgp,
                BestForSubject(sub.Id, s.Id)
            )).ToArray()
        )).ToArray();

        var banners = campaigns
            .OrderByDescending(c => c.DiscountPct)
            .Select(c => new ActiveCampaignBannerDto(
                c.Id, c.Title, c.DiscountPct,
                c.Scope.ToString(),
                c.Scope == CampaignScope.Stage && c.ScopeId.HasValue && stageNames.TryGetValue(c.ScopeId.Value, out var sn) ? sn
                : c.Scope == CampaignScope.Subject && c.ScopeId.HasValue && allSubjects.TryGetValue(c.ScopeId.Value, out var subn) ? subn
                : null,
                c.ValidUntilUtc
            ))
            .ToArray();

        return Result.Success(new PricingResponse(stageDtos, banners));
    }
}
