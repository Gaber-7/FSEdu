namespace FSEdu.Api.LiveKit;

public sealed class LiveKitOptions
{
    public const string SectionName = "LiveKit";
    // Public WS URL returned to browsers (e.g. wss://host/livekit)
    public string Url { get; set; } = "ws://localhost:7880";
    // Optional internal HTTP base for server-to-server Twirp calls (Egress, RoomService).
    // If empty, Url is converted to http(s) at call site.
    public string HttpUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    // Optional override for where the API serves recordings publicly (defaults to /recordings)
    public string RecordingsPath { get; set; } = "";
}
