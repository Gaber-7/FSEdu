namespace FSEdu.Shared.Contracts.Reference;

public sealed record StageDto(int Id, string NameAr, string? NameEn, int LevelOrder, int? ParentStageId);

public sealed record SubjectDto(int Id, int StageId, string NameAr, string? NameEn, string? IconUrl, string? ColorHex);

public sealed record RegionDto(int Id, string NameAr, string? NameEn, string Code);

public sealed record SchoolDto(int Id, string Name, int RegionId, string Type);
