namespace AtelieBebe.Orders.Core.Infrastructure;

/// <summary>
/// The app's own public origins — needed for building URLs that get handed to third parties
/// (payment gateway redirect/webhook), where a relative path won't do. Frontend and API share
/// one origin in production (Nginx proxies /api/* to the backend), but differ in local dev
/// (ng serve on :4200, dotnet run on :5120) — same split resolveAssetUrl() handles client-side.
/// </summary>
public sealed class AppUrlOptions
{
    public const string SectionName = "App";

    /// <summary>The Angular app's own public origin, e.g. https://layettebaby.com.br.</summary>
    public string PublicUrl { get; set; } = "http://localhost:4200";

    /// <summary>The API's own public origin (webhooks are called directly, not through the SPA).</summary>
    public string ApiPublicUrl { get; set; } = "http://localhost:5120";
}
