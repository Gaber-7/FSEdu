using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Notifications;
using FSEdu.Shared.Contracts.Auth;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Notifications;

// ─── Get my preferences ──────────────────────────────
public sealed record GetMyNotificationPreferencesQuery() : IQuery<List<NotificationPreferenceDto>>;

public sealed class GetMyNotificationPreferencesHandler
    : IQueryHandler<GetMyNotificationPreferencesQuery, List<NotificationPreferenceDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyNotificationPreferencesHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<NotificationPreferenceDto>>> Handle(
        GetMyNotificationPreferencesQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var stored = await _db.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Category, p => p.Enabled, ct);

        // Compose all known categories with stored value or default (true).
        var list = NotificationCategories.All.Select(cat =>
        {
            var meta = Catalog[cat];
            var enabled = stored.TryGetValue(cat, out var v) ? v : true;
            return new NotificationPreferenceDto(cat, meta.title, meta.description, meta.emoji, enabled);
        }).ToList();

        return Result.Success(list);
    }

    private static readonly Dictionary<string, (string title, string description, string emoji)> Catalog = new()
    {
        [NotificationCategories.Subscriptions] = ("الاشتراكات والمدفوعات",
            "تفعيل اشتراك، اعتماد دفعة، اشتراك ينتهي قريبًا", "💳"),
        [NotificationCategories.Courses] = ("الدورات",
            "اعتماد دورة، إعلانات من المدرّسين", "📚"),
        [NotificationCategories.LiveSessions] = ("الحصص المباشرة",
            "تذكير قبل الحصة، بدء الحصة الآن", "🎥"),
        [NotificationCategories.QA] = ("الأسئلة والإجابات",
            "ردود على أسئلتك، أسئلة جديدة على دروسك", "❓"),
        [NotificationCategories.Achievements] = ("الإنجازات",
            "شارات جديدة، تغيّر في الترتيب", "🏆"),
        [NotificationCategories.Support] = ("الدعم والتذاكر",
            "ردود على تذاكرك، حلّ التذكرة", "💬"),
        [NotificationCategories.Announcements] = ("الإعلانات العامة",
            "بثّ من الإدارة، تحديثات هامّة", "📣"),
    };
}

// ─── Update one preference ───────────────────────────
public sealed record UpdateNotificationPreferenceCommand(string Category, bool Enabled) : ICommand;

public sealed class UpdateNotificationPreferenceValidator : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    public UpdateNotificationPreferenceValidator()
    {
        RuleFor(x => x.Category).NotEmpty().Must(c => NotificationCategories.All.Contains(c))
            .WithMessage("فئة غير معروفة");
    }
}

public sealed class UpdateNotificationPreferenceHandler : ICommandHandler<UpdateNotificationPreferenceCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateNotificationPreferenceHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateNotificationPreferenceCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var existing = await _db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Category == request.Category, ct);

        if (existing is null)
        {
            // Only store when diverging from default (true).
            if (request.Enabled) return Result.Success();
            _db.NotificationPreferences.Add(new NotificationPreference(userId, request.Category, false));
        }
        else
        {
            existing.Set(request.Enabled);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
