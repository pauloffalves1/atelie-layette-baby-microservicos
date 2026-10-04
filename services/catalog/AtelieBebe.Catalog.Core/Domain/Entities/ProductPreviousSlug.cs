namespace AtelieBebe.Catalog.Core.Domain.Entities;

/// <summary>
/// A link a <see cref="Product"/> used to have before an admin changed its <see cref="Product.Slug"/>.
/// Kept so the old /produto/{slug} address (already indexed by Google, shared on WhatsApp, sent in
/// e-mails) keeps reaching the product instead of a 404, and so no other product can take it over.
/// Owned by Product, same pattern as <see cref="ProductImage"/>.
/// </summary>
public sealed class ProductPreviousSlug
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = default!;
    public DateTime ChangedAt { get; private set; }

    private ProductPreviousSlug() { } // EF Core

    public ProductPreviousSlug(string slug)
    {
        Slug = slug;
        ChangedAt = DateTime.UtcNow;
    }
}
