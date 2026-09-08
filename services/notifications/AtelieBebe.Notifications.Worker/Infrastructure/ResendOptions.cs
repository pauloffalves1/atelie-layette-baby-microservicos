namespace AtelieBebe.Notifications.Worker.Infrastructure;

public sealed class ResendOptions
{
    public const string SectionName = "Resend";

    /// <summary>API key from resend.com/api-keys. Blank in appsettings.json — set via dotnet user-secrets.</summary>
    public string ApiKey { get; set; } = default!;

    /// <summary>Must be on a domain verified in the Resend dashboard (DNS records), or sending fails.</summary>
    public string FromEmail { get; set; } = "Ateliê Layette Baby <pedidos@layettebaby.com.br>";
}
