namespace FSEdu.Shared.Contracts.Search;

// One result row — kind tells the UI which icon/route to use
public sealed record SearchResultDto(
    string Kind,           // "course" | "live" | "recording" | "lesson"
    Guid Id,
    string Title,
    string? Subtitle,      // e.g. subject + teacher name
    string? Snippet,       // short context
    string? Url,           // navigation target (relative)
    string? ThumbnailUrl,
    DateTime? AtUtc        // for sorting / display (course created, session scheduled, etc.)
);

public sealed record SearchResponseDto(
    string Query,
    int TotalResults,
    List<SearchResultDto> Courses,
    List<SearchResultDto> LiveSessions,
    List<SearchResultDto> Recordings,
    List<SearchResultDto> Lessons
);
