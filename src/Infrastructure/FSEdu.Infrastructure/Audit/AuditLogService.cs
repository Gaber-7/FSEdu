using System.Text.Json;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Infrastructure.Audit;

// Default implementation: persists entries to the AuditEntries table.
// Resolves the actor from ICurrentUser and the IP from IHttpContextAccessor.
// All operations are best-effort — exceptions are swallowed so a failure
// to log NEVER breaks the calling business action.
public sealed class AuditLogService : IAuditLog
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _http;

    public AuditLogService(IApplicationDbContext db, ICurrentUser currentUser, IHttpContextAccessor http)
    {
        _db = db; _currentUser = currentUser; _http = http;
    }

    public async Task RecordAsync(string action, string? entityType = null, string? entityId = null,
                                   object? details = null, CancellationToken ct = default)
    {
        try
        {
            if (_currentUser.UserId is null) return;
            var userId = _currentUser.UserId.Value;

            var name = await _db.Users.Where(u => u.Id == userId)
                .Select(u => u.FullName).FirstOrDefaultAsync(ct) ?? "system";
            var role = (_currentUser.IsInRole("Admin") ? "Admin"
                     : _currentUser.IsInRole("Teacher") ? "Teacher"
                     : _currentUser.IsInRole("Parent") ? "Parent"
                     : _currentUser.IsInRole("Supervisor") ? "Supervisor"
                     : "Student");

            string? ip = null;
            var ctx = _http.HttpContext;
            if (ctx is not null)
            {
                ip = ctx.Connection?.RemoteIpAddress?.ToString();
                if (ip is not null && ip.Length > 64) ip = ip.Substring(0, 64);
            }

            var json = details is null ? null : JsonSerializer.Serialize(details);
            _db.AuditEntries.Add(new AuditEntry(
                userId, name, role, action, entityType, entityId, json, ip));
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // Audit logging is best-effort. Do NOT propagate.
        }
    }
}
