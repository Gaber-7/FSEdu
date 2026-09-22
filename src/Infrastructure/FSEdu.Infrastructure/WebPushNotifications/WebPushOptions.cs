namespace FSEdu.Infrastructure.WebPushNotifications;

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";
    /// <summary>Contact for push services — typically `mailto:admin@yourdomain.com`.</summary>
    public string Subject { get; set; } = "mailto:admin@fsedu.local";
    /// <summary>VAPID public key (URL-safe base64). Generate with WebPush.VapidHelper.</summary>
    public string PublicKey { get; set; } = "";
    public string PrivateKey { get; set; } = "";
}
