namespace AtelieBebe.Notifications.Worker.Infrastructure;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>Permanent access token for the Meta WhatsApp Business Cloud API. Blank in appsettings.json — set via dotnet user-secrets.</summary>
    public string AccessToken { get; set; } = default!;

    /// <summary>The Meta phone number ID (not the phone number itself) the messages are sent from.</summary>
    public string PhoneNumberId { get; set; } = default!;

    /// <summary>Graph API version, e.g. "v21.0".</summary>
    public string ApiVersion { get; set; } = "v21.0";
}
