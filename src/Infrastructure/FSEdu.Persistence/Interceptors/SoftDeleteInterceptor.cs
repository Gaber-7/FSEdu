using FSEdu.Application.Abstractions;
using FSEdu.Shared.Kernel.Primitives;
using FSEdu.Shared.Kernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FSEdu.Persistence.Interceptors;

public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser? _currentUser;

    public SoftDeleteInterceptor(IDateTimeProvider clock, ICurrentUser? currentUser)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in eventData.Context.ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State != EntityState.Deleted) continue;

            entry.State = EntityState.Modified;
            entry.Entity.DeletedAtUtc = _clock.UtcNow;
            entry.Entity.DeletedBy = _currentUser?.UserId?.ToString();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
