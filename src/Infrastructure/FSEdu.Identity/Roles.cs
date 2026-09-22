namespace FSEdu.Identity;

public static class Roles
{
    public const string Student = "Student";
    public const string Parent = "Parent";
    public const string Teacher = "Teacher";
    public const string Assistant = "Assistant";
    public const string Admin = "Admin";
    public const string Supervisor = "Supervisor";

    public static readonly string[] All = { Student, Parent, Teacher, Assistant, Admin, Supervisor };
}
