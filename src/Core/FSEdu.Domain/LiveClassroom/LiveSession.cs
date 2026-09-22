using FSEdu.Domain.Common;
using FSEdu.Domain.Courses;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.LiveClassroom;

public sealed class LiveSession : AggregateRoot<Guid>
{
    public Guid? LessonId { get; private set; }
    public Lesson? Lesson { get; private set; }
    public Guid TeacherId { get; private set; }
    public Teacher Teacher { get; private set; } = default!;
    public int? SubjectId { get; private set; }   // for subject-level access check
    public int? StageId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string RoomId { get; private set; } = default!;
    public LiveSessionStatus Status { get; private set; } = LiveSessionStatus.Scheduled;
    public DateTime ScheduledAtUtc { get; private set; }
    public int DurationMinutes { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public string? RecordingUrl { get; private set; }
    public string? RecordingStatus { get; private set; }
    public int MaxParticipants { get; private set; }

    private readonly List<LiveAttendance> _attendance = new();
    public IReadOnlyCollection<LiveAttendance> Attendance => _attendance.AsReadOnly();

    private LiveSession() { }

    public LiveSession(Guid id, Guid teacherId, string title, int subjectId, int stageId,
                        string roomId, DateTime scheduledAtUtc, int durationMinutes,
                        Guid? lessonId = null, string? description = null) : base(id)
    {
        TeacherId = teacherId;
        Title = title;
        SubjectId = subjectId;
        StageId = stageId;
        RoomId = roomId;
        ScheduledAtUtc = scheduledAtUtc;
        DurationMinutes = durationMinutes;
        LessonId = lessonId;
        Description = description;
    }

    public void Start()
    {
        if (Status != LiveSessionStatus.Scheduled) return;
        Status = LiveSessionStatus.Live;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void End()
    {
        if (Status != LiveSessionStatus.Live) return;
        Status = LiveSessionStatus.Ended;
        EndedAtUtc = DateTime.UtcNow;
    }

    public void Cancel() => Status = LiveSessionStatus.Cancelled;

    public void UpdateScheduleDetails(
        string title, string? description,
        int subjectId, int stageId,
        DateTime scheduledAtUtc, int durationMinutes)
    {
        if (Status != LiveSessionStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled sessions can be edited.");
        Title = title;
        Description = description;
        SubjectId = subjectId;
        StageId = stageId;
        ScheduledAtUtc = scheduledAtUtc;
        DurationMinutes = durationMinutes;
    }

    public void AttachRecording(string url)
    {
        RecordingUrl = url;
        RecordingStatus = "Ready";
    }

    public void StudentJoined(Guid studentId)
    {
        var existing = _attendance.FirstOrDefault(a => a.StudentId == studentId);
        if (existing is null)
            _attendance.Add(new LiveAttendance(Id, studentId));
        else existing.ReJoin();

        if (_attendance.Count > MaxParticipants) MaxParticipants = _attendance.Count;
    }

    public void StudentLeft(Guid studentId)
    {
        var record = _attendance.FirstOrDefault(a => a.StudentId == studentId);
        record?.Leave();
    }
}

public sealed class LiveAttendance
{
    public Guid SessionId { get; private set; }
    public Guid StudentId { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }
    public DateTime? LeftAtUtc { get; private set; }
    public int TotalDurationSec { get; private set; }
    public decimal AttentionScore { get; private set; }

    private LiveAttendance() { }

    public LiveAttendance(Guid sessionId, Guid studentId)
    {
        SessionId = sessionId;
        StudentId = studentId;
        JoinedAtUtc = DateTime.UtcNow;
    }

    public void Leave()
    {
        LeftAtUtc = DateTime.UtcNow;
        TotalDurationSec += (int)(LeftAtUtc.Value - JoinedAtUtc).TotalSeconds;
    }

    public void ReJoin()
    {
        LeftAtUtc = null;
        JoinedAtUtc = DateTime.UtcNow;
    }

    public void SetAttentionScore(decimal score) => AttentionScore = Math.Clamp(score, 0, 100);
}
