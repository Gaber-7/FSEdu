using FSEdu.Api.Endpoints;
using FSEdu.Api.Seeding;
using FSEdu.Application;
using FSEdu.Identity;
using FSEdu.Identity.Seeding;
using FSEdu.Infrastructure;
using FSEdu.Persistence;
using FSEdu.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Allow up to 2 GB recording uploads (a 60-min lecture is ~300-500 MB)
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024;
    o.ValueLengthLimit = int.MaxValue;
});

builder.Services.AddOpenApi();

// Payment accounts config
builder.Services.Configure<FSEdu.Api.Endpoints.PaymentAccountsOptions>(
    builder.Configuration.GetSection(FSEdu.Api.Endpoints.PaymentAccountsOptions.SectionName));

// LiveKit config + token service + egress (recording) service
builder.Services.Configure<FSEdu.Api.LiveKit.LiveKitOptions>(
    builder.Configuration.GetSection(FSEdu.Api.LiveKit.LiveKitOptions.SectionName));
builder.Services.AddSingleton<FSEdu.Api.LiveKit.ILiveKitTokenService, FSEdu.Api.LiveKit.LiveKitTokenService>();
builder.Services.AddHttpClient("livekit-egress");
builder.Services.AddSingleton<FSEdu.Application.Abstractions.IEgressService, FSEdu.Api.LiveKit.LiveKitEgressService>();

// Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddInfrastructure();
builder.Services.Configure<FSEdu.Infrastructure.WebPushNotifications.WebPushOptions>(
    builder.Configuration.GetSection(FSEdu.Infrastructure.WebPushNotifications.WebPushOptions.SectionName));

// SignalR for live classroom
builder.Services.AddSignalR();

// Auth
builder.Services.AddJwtAuthentication(builder.Configuration);

// Allow JWT auth via SignalR query string (?access_token=)
builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, options =>
{
    var existing = options.Events?.OnMessageReceived;
    options.Events ??= new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents();
    options.Events.OnMessageReceived = async ctx =>
    {
        if (existing is not null) await existing(ctx);
        var accessToken = ctx.Request.Query["access_token"];
        var path = ctx.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            ctx.Token = accessToken;
    };
});

// CORS for Blazor client
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
        policy.WithOrigins("https://localhost:7100", "http://localhost:5100")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    //await app.Services.EnsureIdentityDbAsync();
    await IdentitySeeder.SeedAdminAsync(app.Services);
    FSEdu.Infrastructure.WebPushNotifications.VapidKeyBootstrapper.EnsureDevKeys(
        app.Services, app.Environment);
    await DataSeeder.SeedAsync(app.Services);
    await DevUsersSeeder.SeedAsync(app.Services);
    await DevContentSeeder.SeedAsync(app.Services);
    await DevExtendedSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();
app.UseStaticFiles();  // Serve uploaded receipts from wwwroot

// Serve LiveKit Egress recordings (mounted as /recordings volume in production)
var recordingsPath = builder.Configuration["LiveKit:RecordingsPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "recordings");
if (!Directory.Exists(recordingsPath)) Directory.CreateDirectory(recordingsPath);
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(recordingsPath),
    RequestPath = "/recordings",
    ServeUnknownFileTypes = false
});

app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    name = "FSEdu API",
    version = "v1",
    status = "running",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Feature endpoints
app.MapAuthEndpoints();
app.MapReferenceEndpoints();
app.MapAdminEndpoints();
app.MapParentEndpoints();
app.MapSubscriptionEndpoints();
app.MapCourseEndpoints();
app.MapDashboardEndpoints();
app.MapAssessmentEndpoints();
app.MapPaymentEndpoints();
app.MapTicketEndpoints();
app.MapNotificationEndpoints();
app.MapLiveClassroomEndpoints();
app.MapGamificationEndpoints();
app.MapSearchEndpoints();
app.MapTeacherEndpoints();
app.MapSecurityEndpoints();
app.MapPastPaperEndpoints();
app.MapHomeworkEndpoints();

// SignalR Hub
app.MapHub<FSEdu.Api.Hubs.ClassroomHub>("/hubs/classroom").RequireAuthorization();

app.Run();
