namespace FSEdu.Shared.Kernel.Primitives;

public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    string? UpdatedBy { get; set; }
}

public interface ISoftDelete
{
    DateTime? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
    bool IsDeleted => DeletedAtUtc.HasValue;
}
