using System.Text;
using FSEdu.Application.Abstractions;
using FSEdu.Identity.Entities;
using FSEdu.Identity.Otp;
using FSEdu.Identity.Persistence;
using FSEdu.Identity.Services;
using FSEdu.Identity.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FSEdu.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services,
                                                                IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing");

        services.AddDbContext<IdentityDbContext>(o =>
            o.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "auth")));

        services.AddIdentityCore<ApplicationUser>(opts =>
        {
            opts.User.RequireUniqueEmail = false;
            opts.Password.RequiredLength = 8;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequireDigit = true;
            opts.Password.RequireUppercase = false;
            opts.Password.RequireLowercase = false;
            opts.SignIn.RequireConfirmedPhoneNumber = false;
            opts.Lockout.MaxFailedAccessAttempts = 10;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ISmsSender, ConsoleSmsSender>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<ITotpService, TotpService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
                                                           IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static async Task EnsureIdentityDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new ApplicationRole(role));
        }
    }
}
