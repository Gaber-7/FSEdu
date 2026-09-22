using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Auth;

namespace FSEdu.Application.Features.Auth.RegisterStudent;

public sealed record RegisterStudentCommand(
    string FullName,
    string Phone,
    string? Email,
    string Password,
    int StageId,
    int RegionId,
    int? SchoolId,
    string? ParentPhone
) : ICommand<AuthResponse>;
