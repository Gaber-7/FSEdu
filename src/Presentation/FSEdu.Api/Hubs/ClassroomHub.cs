using System.Collections.Concurrent;
using System.Security.Claims;
using FSEdu.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Api.Hubs;

[Authorize]
public sealed class ClassroomHub : Hub
{
    private readonly IApplicationDbContext _db;

    // In-memory tracking — production should use Redis
    private static readonly ConcurrentDictionary<string, ParticipantInfo> _participants = new();

    // Per-session whiteboard stroke history. Late joiners receive a snapshot.
    // Each entry is the raw JSON the teacher's client sent — opaque to the server.
    private static readonly ConcurrentDictionary<string, List<object>> _whiteboards = new();

    // Per-session active poll. One poll at a time per session.
    private sealed class ActivePoll
    {
        public required string PollId { get; init; }
        public required string Question { get; init; }
        public required string[] Options { get; init; }
        public required int[] Counts { get; init; }
        public ConcurrentDictionary<Guid, int> VotesByUser { get; } = new(); // userId -> optionIndex
        public bool Closed { get; set; }
    }
    private static readonly ConcurrentDictionary<string, ActivePoll> _polls = new();

    public ClassroomHub(IApplicationDbContext db) => _db = db;

    private record ParticipantInfo(Guid UserId, string UserName, string SessionId, bool IsTeacher,
                                    bool HandRaised, DateTime? HandRaisedAtUtc = null);

    public async Task JoinRoom(string sessionIdStr)
    {
        if (!Guid.TryParse(sessionIdStr, out var sessionId))
            throw new HubException("Invalid session ID");

        var session = await _db.LiveSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
            throw new HubException("Session not found");

        var userId = GetUserId();
        var userName = GetUserName();
        var isTeacher = session.TeacherId == userId;

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionIdStr);

        _participants[Context.ConnectionId] = new ParticipantInfo(userId, userName, sessionIdStr, isTeacher, false);

        // Notify others
        await Clients.OthersInGroup(sessionIdStr).SendAsync("UserJoined", new
        {
            userId, userName, isTeacher
        });

        // Send current participants to the new joiner
        var roster = _participants
            .Where(p => p.Value.SessionId == sessionIdStr && p.Key != Context.ConnectionId)
            .Select(p => new { p.Value.UserId, p.Value.UserName, p.Value.IsTeacher, p.Value.HandRaised, p.Value.HandRaisedAtUtc })
            .ToList();

        await Clients.Caller.SendAsync("Roster", roster);

        // Send current whiteboard snapshot so the new joiner can replay
        if (_whiteboards.TryGetValue(sessionIdStr, out var strokes) && strokes.Count > 0)
        {
            // Send a copy under lock to avoid concurrent modification
            List<object> snapshot;
            lock (strokes) { snapshot = new List<object>(strokes); }
            await Clients.Caller.SendAsync("WhiteboardSnapshot", snapshot);
        }

        // Send active poll (if any) so late joiners see it
        if (_polls.TryGetValue(sessionIdStr, out var poll) && !poll.Closed)
        {
            await Clients.Caller.SendAsync("PollStarted", new
            {
                pollId = poll.PollId,
                question = poll.Question,
                options = poll.Options,
                counts = poll.Counts,
                myVote = poll.VotesByUser.TryGetValue(userId, out var v) ? (int?)v : null
            });
        }
    }

    // ─── Whiteboard ────────────────────────────────────────
    // Teacher draws → DrawStroke broadcasts to others + server records for late joiners.
    // Stroke payload is opaque (JSON object) — keys like color/width/points decided by client.

    public async Task DrawStroke(string sessionIdStr, object stroke)
    {
        if (!_participants.TryGetValue(Context.ConnectionId, out var info)) return;
        if (info.SessionId != sessionIdStr || !info.IsTeacher) return;

        var strokes = _whiteboards.GetOrAdd(sessionIdStr, _ => new List<object>());
        lock (strokes)
        {
            strokes.Add(stroke);
            // Cap history to prevent unbounded growth (60-min lecture might draw a lot).
            // 5000 strokes is plenty; oldest dropped first.
            if (strokes.Count > 5000) strokes.RemoveRange(0, strokes.Count - 5000);
        }
        await Clients.OthersInGroup(sessionIdStr).SendAsync("StrokeReceived", stroke);
    }

    // ─── Live Polls ────────────────────────────────────────
    public async Task CreatePoll(string sessionIdStr, string question, string[] options)
    {
        if (!_participants.TryGetValue(Context.ConnectionId, out var info)) return;
        if (info.SessionId != sessionIdStr || !info.IsTeacher) return;
        if (string.IsNullOrWhiteSpace(question)) return;
        if (options is null || options.Length < 2 || options.Length > 6) return;

        var poll = new ActivePoll
        {
            PollId = Guid.NewGuid().ToString("N"),
            Question = question.Trim(),
            Options = options.Select(o => (o ?? "").Trim()).ToArray(),
            Counts = new int[options.Length]
        };
        _polls[sessionIdStr] = poll;

        await Clients.Group(sessionIdStr).SendAsync("PollStarted", new
        {
            pollId = poll.PollId,
            question = poll.Question,
            options = poll.Options,
            counts = poll.Counts,
            myVote = (int?)null
        });
    }

    public async Task VoteOnPoll(string sessionIdStr, string pollId, int optionIndex)
    {
        if (!_participants.TryGetValue(Context.ConnectionId, out var info)) return;
        if (info.SessionId != sessionIdStr) return;
        if (!_polls.TryGetValue(sessionIdStr, out var poll)) return;
        if (poll.Closed || poll.PollId != pollId) return;
        if (optionIndex < 0 || optionIndex >= poll.Options.Length) return;

        // First-time vote → just count. Changing vote → adjust counts.
        if (poll.VotesByUser.TryGetValue(info.UserId, out var prev))
        {
            if (prev == optionIndex) return; // no change
            Interlocked.Decrement(ref poll.Counts[prev]);
        }
        Interlocked.Increment(ref poll.Counts[optionIndex]);
        poll.VotesByUser[info.UserId] = optionIndex;

        await Clients.Group(sessionIdStr).SendAsync("PollVoteUpdate", new
        {
            pollId = poll.PollId,
            counts = poll.Counts
        });
    }

    public async Task ClosePoll(string sessionIdStr)
    {
        if (!_participants.TryGetValue(Context.ConnectionId, out var info)) return;
        if (info.SessionId != sessionIdStr || !info.IsTeacher) return;
        if (!_polls.TryGetValue(sessionIdStr, out var poll)) return;
        poll.Closed = true;
        await Clients.Group(sessionIdStr).SendAsync("PollClosed", new
        {
            pollId = poll.PollId,
            counts = poll.Counts
        });
    }

    public async Task ClearWhiteboard(string sessionIdStr)
    {
        if (!_participants.TryGetValue(Context.ConnectionId, out var info)) return;
        if (info.SessionId != sessionIdStr || !info.IsTeacher) return;

        if (_whiteboards.TryGetValue(sessionIdStr, out var strokes))
        {
            lock (strokes) { strokes.Clear(); }
        }
        await Clients.OthersInGroup(sessionIdStr).SendAsync("WhiteboardCleared");
    }

    public async Task SendMessage(string sessionIdStr, string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return;
        if (!_participants.TryGetValue(Context.ConnectionId, out var info)) return;
        if (info.SessionId != sessionIdStr) return;

        await Clients.Group(sessionIdStr).SendAsync("MessageReceived", new
        {
            senderId = info.UserId,
            senderName = info.UserName,
            isTeacher = info.IsTeacher,
            body = body.Trim(),
            sentAtUtc = DateTime.UtcNow
        });
    }

    public async Task RaiseHand(string sessionIdStr, bool raised)
    {
        if (!_participants.TryGetValue(Context.ConnectionId, out var info)) return;
        if (info.SessionId != sessionIdStr) return;

        var updated = info with
        {
            HandRaised = raised,
            HandRaisedAtUtc = raised ? DateTime.UtcNow : null
        };
        _participants[Context.ConnectionId] = updated;

        await Clients.Group(sessionIdStr).SendAsync("HandRaised", new
        {
            userId = info.UserId,
            userName = info.UserName,
            raised,
            handRaisedAtUtc = updated.HandRaisedAtUtc
        });
    }

    // Teacher gives the floor to a raised-hand student. Lowers their hand
    // automatically and notifies everyone so a banner can be shown.
    public async Task CallOnStudent(string sessionIdStr, string studentUserId)
    {
        if (!_participants.TryGetValue(Context.ConnectionId, out var caller)) return;
        if (caller.SessionId != sessionIdStr || !caller.IsTeacher) return;
        if (!Guid.TryParse(studentUserId, out var sId)) return;

        var entry = _participants
            .Where(p => p.Value.SessionId == sessionIdStr && p.Value.UserId == sId)
            .Select(p => (Key: p.Key, Value: p.Value))
            .FirstOrDefault();
        if (entry.Value is null) return;

        // Lower the called student's hand
        var lowered = entry.Value with { HandRaised = false, HandRaisedAtUtc = null };
        _participants[entry.Key] = lowered;

        await Clients.Group(sessionIdStr).SendAsync("StudentCalledOn", new
        {
            userId = entry.Value.UserId,
            userName = entry.Value.UserName,
            byTeacher = caller.UserName
        });
        await Clients.Group(sessionIdStr).SendAsync("HandRaised", new
        {
            userId = entry.Value.UserId,
            userName = entry.Value.UserName,
            raised = false,
            handRaisedAtUtc = (DateTime?)null
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_participants.TryRemove(Context.ConnectionId, out var info))
        {
            await Clients.OthersInGroup(info.SessionId).SendAsync("UserLeft", new
            {
                userId = info.UserId,
                userName = info.UserName
            });

            // Record leave time
            try
            {
                if (Guid.TryParse(info.SessionId, out var sId))
                {
                    var session = await _db.LiveSessions
                        .Include(s => s.Attendance)
                        .FirstOrDefaultAsync(s => s.Id == sId);
                    if (session is not null && !info.IsTeacher)
                    {
                        session.StudentLeft(info.UserId);
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch { /* swallow disconnect cleanup errors */ }
        }
        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string GetUserName()
    {
        return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "مستخدم";
    }
}
