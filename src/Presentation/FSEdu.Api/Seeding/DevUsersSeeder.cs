using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Identity.Entities;
using FSEdu.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Api.Seeding;

public static class DevUsersSeeder
{
    private record UserSpec(string Phone, string Role, string FullName, string? Email);

    private static readonly UserSpec[] Users = new[]
    {
        new UserSpec("+20115155691", "Student", "أحمد الطالب التجريبي", "student@fsedu.local"),
        new UserSpec("+20115155692", "Parent",  "محمد ولي الأمر التجريبي", "parent@fsedu.local"),
        new UserSpec("+20115155693", "Teacher", "الأستاذ علي المدرس",      "teacher@fsedu.local"),
        new UserSpec("+20115155694", "Admin",   "المشرف الإداري",           "admin2@fsedu.local"),
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var domain = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevUsersSeeder");

        var stage = await domain.Stages.Where(s => s.ParentStageId != null).FirstAsync();
        var region = await domain.Regions.FirstAsync();
        var school = await domain.Schools.FirstOrDefaultAsync();
        var subject = await domain.Subjects.FirstAsync(s => s.StageId == stage.Id);

        foreach (var spec in Users)
        {
            var password = "0" + spec.Phone.Substring(3); // +20115155691 → 0115155691

            var existing = await userMgr.FindByNameAsync(spec.Phone);
            if (existing is not null)
            {
                log.LogInformation("Dev user already exists: {Phone} ({Role})", spec.Phone, spec.Role);
                continue;
            }

            if (!await roleMgr.RoleExistsAsync(spec.Role))
                await roleMgr.CreateAsync(new ApplicationRole(spec.Role));

            var id = Guid.NewGuid();
            var authUser = new ApplicationUser
            {
                Id = id,
                UserName = spec.Phone,
                PhoneNumber = spec.Phone,
                PhoneNumberConfirmed = true,
                Email = spec.Email,
                EmailConfirmed = true,
                FullName = spec.FullName,
                DomainUserId = id
            };

            var createResult = await userMgr.CreateAsync(authUser, password);
            if (!createResult.Succeeded)
            {
                log.LogError("Failed to create {Phone}: {Err}", spec.Phone,
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                continue;
            }

            await userMgr.AddToRoleAsync(authUser, spec.Role);

            var phoneVo = PhoneNumber.Create(spec.Phone).Value;
            var email = spec.Email is null ? null : Email.Create(spec.Email).Value;

            switch (spec.Role)
            {
                case "Student":
                    var student = new Student(id, spec.FullName, phoneVo, stage.Id, region.Id, school?.Id, email);
                    student.VerifyPhone();
                    student.Activate();
                    domain.Students.Add(student);
                    break;

                case "Parent":
                    var parent = new Parent(id, spec.FullName, phoneVo, email, "29912345678901", "موظف");
                    parent.VerifyPhone();
                    parent.Activate();
                    domain.Parents.Add(parent);
                    break;

                case "Teacher":
                    var teacher = new Teacher(id, spec.FullName, phoneVo,
                        yearsOfExperience: 8,
                        bio: "مدرس تجريبي لاختبار المنصة.",
                        email: email);
                    teacher.AddSubject(subject.Id);
                    teacher.AddRegion(region.Id);
                    teacher.AddQualification("بكالوريوس تربية - رياضيات", "جامعة القاهرة", 2015, null);
                    teacher.VerifyPhone();
                    teacher.Verify(Guid.Empty);
                    domain.Teachers.Add(teacher);
                    break;
            }

            await domain.SaveChangesAsync();
            log.LogWarning("🔐 [DEV USER] {Role,-8} | Phone: {Phone} | Password: {Password}",
                spec.Role, spec.Phone, password);
        }

        log.LogWarning("══════════════════════════════════════════════════════");
        log.LogWarning("🎉 حسابات التطوير جاهزة! استخدم البيانات التالية للدخول:");
        log.LogWarning("   🎓 طالب     : +20115155691 / 0115155691");
        log.LogWarning("   👨‍👩‍👦 ولي أمر  : +20115155692 / 0115155692");
        log.LogWarning("   👨‍🏫 مدرس     : +20115155693 / 0115155693");
        log.LogWarning("   ⚙️  أدمن    : +20115155694 / 0115155694");
        log.LogWarning("══════════════════════════════════════════════════════");
    }
}
