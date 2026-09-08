namespace AtelieBebe.Catalog.Core.Domain.Entities;

/// <summary>
/// One additional gallery photo for a <see cref="Product"/>, beyond its <see cref="Product.ImageUrl"/>
/// cover photo. Owned by Product — never queried or referenced on its own, same pattern as
/// <see cref="ProductCustomerAccessEntry"/>.
/// </summary>
public sealed class ProductImage
{
    public Guid Id { get; private set; }
    public string Url { get; private set; } = default!;
    public int SortOrder { get; private set; }

    private ProductImage() { } // EF Core

    public ProductImage(string url, int sortOrder)
    {
        Url = url;
        SortOrder = sortOrder;
    }
}
