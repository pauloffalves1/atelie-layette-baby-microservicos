namespace AtelieBebe.Catalog.Core.Infrastructure;

/// <summary>
/// The app's own public origin — needed for building URLs handed to customers in e-mails
/// (e.g. the wishlist reminder's product link), where a relative path won't do.
/// </summary>
public sealed class AppUrlOptions
{
    public const string SectionName = "App";

    /// <summary>The Angular app's own public origin, e.g. https://layettebaby.com.br.</summary>
    public string PublicUrl { get; set; } = "http://localhost:4200";
}
