using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSEdu.Infrastructure.WebPushNotifications;

// On dev startup, if VAPID keys are missing from configuration, generate a fresh
// keypair and persist it to a local file so subsequent runs reuse the same keys.
public static class VapidKeyBootstrapper
{
    private const string KeyFileName = "vapid-keys.json";

    public static void EnsureDevKeys(IServiceProvider services, IHostEnvironment env)
    {
        if (!env.IsDevelopment()) return;

        var opts = services.GetRequiredService<IOptions<WebPushOptions>>().Value;
        var log = services.GetRequiredService<ILoggerFactory>().CreateLogger("VapidBootstrap");
        if (!string.IsNullOrEmpty(opts.PublicKey) && !string.IsNullOrEmpty(opts.PrivateKey)) return;

        var path = Path.Combine(env.ContentRootPath, KeyFileName);
        string publicKey, privateKey;

        if (File.Exists(path))
        {
            using var fs = File.OpenRead(path);
            using var doc = JsonDocument.Parse(fs);
            publicKey = doc.RootElement.GetProperty("PublicKey").GetString() ?? "";
            privateKey = doc.RootElement.GetProperty("PrivateKey").GetString() ?? "";
            log.LogInformation("🔑 [DEV] Loaded VAPID keys from {Path}", path);
        }
        else
        {
            var pair = global::WebPush.VapidHelper.GenerateVapidKeys();
            publicKey = pair.PublicKey;
            privateKey = pair.PrivateKey;
            File.WriteAllText(path, JsonSerializer.Serialize(new { PublicKey = publicKey, PrivateKey = privateKey }));
            log.LogWarning("🔑 [DEV] Generated VAPID keys → saved to {Path}", path);
        }

        opts.PublicKey = publicKey;
        opts.PrivateKey = privateKey;
        log.LogWarning("✅ Web Push enabled (PublicKey: {PublicKey}…)", publicKey.Substring(0, Math.Min(16, publicKey.Length)));
    }
}
