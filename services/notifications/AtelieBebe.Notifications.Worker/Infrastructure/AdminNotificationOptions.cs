namespace AtelieBebe.Notifications.Worker.Infrastructure;

/// <summary>Destination for the "new order" admin alert — separate from AdminSeed (login credentials).</summary>
public sealed class AdminNotificationOptions
{
    public const string SectionName = "AdminNotification";

    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
}
