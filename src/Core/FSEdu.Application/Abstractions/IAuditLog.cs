namespace FSEdu.Application.Abstractions;

// Write-only sink for privileged actions. Implementations should be
// best-effort: a failure to record the audit entry MUST NOT block the
// underlying action (the operation already succeeded by the time we log).
public interface IAuditLog
{
    Task RecordAsync(string action, string? entityType = null, string? entityId = null,
                      object? details = null, CancellationToken ct = default);
}
