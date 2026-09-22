using System.Globalization;
using FSEdu.Web.Components;
using FSEdu.Web.Services;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// ─── Localization ──────────────────────────────
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    var supported = new[] { new CultureInfo("ar"), new CultureInfo("en") };
    o.DefaultRequestCulture = new RequestCulture("ar");
    o.SupportedCultures = supported;
    o.SupportedUICultures = supported;
    // Cookie picks up the user's choice from the language toggle
    o.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

// API client — Scoped per circuit so the auth token persists across pages
builder.Services.AddScoped<ApiClient>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["ApiBaseUrl"] ?? "http://localhost:5080/";
    var http = new HttpClient
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(30)
    };
    return new ApiClient(http);
});

// Auth state (scoped per circuit)
builder.Services.AddScoped<AuthState>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseAntiforgery();

// ─── Language switcher endpoint ────────────────
// Toggling the language reload-redirects the user so the new culture applies
// to the next request. Avoids client-side culture juggling in Blazor Server.
app.MapGet("/set-culture", (string culture, string? returnUrl, HttpContext http) =>
{
    var safeCulture = (culture is "en" or "ar") ? culture : "ar";
    http.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(safeCulture, safeCulture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
    return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(FSEdu.Web.Client._Imports).Assembly);

app.Run();
