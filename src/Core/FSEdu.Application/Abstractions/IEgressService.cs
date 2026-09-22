namespace FSEdu.Application.Abstractions;

public interface IEgressService
{
    Task<string> StartRoomRecordingAsync(string roomName, Guid sessionId, CancellationToken ct);
    Task StopRecordingAsync(Guid sessionId, CancellationToken ct);
    bool IsRecording(Guid sessionId);
}
