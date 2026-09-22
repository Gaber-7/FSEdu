using FSEdu.Identity.Entities;
using FSEdu.Identity.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSEdu.Identity.Seeding;

public static class IdentitySeeder
{
    public const string DefaultAdminPhone = "+201000000000";
    public const string DefaultAdminPassword = "Admin@12345";

    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        if (!await roles.RoleExistsAsync(Roles.Admin))
            await roles.CreateAsync(new ApplicationRole(Roles.Admin));

        var existing = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == DefaultAdminPhone);
        if (existing is not null) return;

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = DefaultAdminPhone,
            PhoneNumber = DefaultAdminPhone,
            PhoneNumberConfirmed = true,
            Email = "admin@fsedu.local",
            EmailConfirmed = true,
            FullName = "المشرف الإداري الأساسي",
        };

        var result = await users.CreateAsync(admin, DefaultAdminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await users.AddToRoleAsync(admin, Roles.Admin);
        logger.LogWarning("🔐 [DEV] Admin seeded | Phone: {Phone} | Password: {Password}",
            DefaultAdminPhone, DefaultAdminPassword);
    }
}
