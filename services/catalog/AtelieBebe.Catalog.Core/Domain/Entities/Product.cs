using AtelieBebe.SharedKernel.Common;
using AtelieBebe.Catalog.Core.Domain.Events;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Catalog.Core.Domain.Entities;

public sealed class Product : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public string? ImageUrl { get; private set; }
    public bool Active { get; private set; }
    public bool Featured { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public decimal? DiscountPercentage { get; private set; }
    public DateTime? PromotionStartsAt { get; private set; }
    public DateTime? PromotionEndsAt { get; private set; }

    private readonly List<ProductCustomerAccessEntry> _allowedCustomerAccess = new();
    private readonly List<ProductImage> _images = new();

    /// <summary>Customers this product is restricted to. Empty means the product is public.</summary>
    public IReadOnlyCollection<Guid> AllowedCustomerIds => _allowedCustomerAccess.Select(e => e.CustomerId).ToList().AsReadOnly();

    /// <summary>Additional gallery photos, beyond the cover photo (<see cref="ImageUrl"/>), in display order.</summary>
    public IReadOnlyList<string> ImageUrls => _images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList();

    /// <summary>A product with at least one allowed customer is exclusive — invisible to everyone else.</summary>
    public bool IsExclusive => _allowedCustomerAccess.Count > 0;

    /// <summary>True only while "now" falls inside the configured promotion window — expires and (re)activates on its own, no background job needed.</summary>
    public bool IsOnPromotion =>
        DiscountPercentage is > 0 && PromotionStartsAt is { } start && PromotionEndsAt is { } end &&
        DateTime.UtcNow >= start && DateTime.UtcNow <= end;

    /// <summary>The price to charge/display right now — the promotional price while the window is active, the regular price otherwise.</summary>
    public Money EffectivePrice => IsOnPromotion
        ? Money.FromReais(Math.Round(Price.Amount * (1 - DiscountPercentage!.Value / 100m), 2))
        : Price;

    private Product() { } // EF Core

    private Product(Guid id, string name, string slug, string? description, Money price,
        string category, string? imageUrl, bool featured) : base(id)
    {
        Name = name;
        Slug = slug;
        Description = description;
        Price = price;
        Category = category;
        ImageUrl = imageUrl;
        Active = true;
        Featured = featured;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Product Create(string name, string slug, string? description, Money price,
        string category, string? imageUrl, bool featured = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do produto é obrigatório.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("O slug do produto é obrigatório.");
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("A categoria do produto é obrigatória.");

        return new Product(Guid.NewGuid(), name.Trim(), slug.Trim().ToLowerInvariant(),
            description, price, category.Trim(), imageUrl, featured);
    }

    public void UpdateDetails(string name, string? description, Money price, string category,
        string? imageUrl, bool featured)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do produto é obrigatório.");

        Name = name.Trim();
        Description = description;
        Price = price;
        Category = category.Trim();
        ImageUrl = imageUrl;
        Featured = featured;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool active)
    {
        if (active && !Active)
            AddDomainEvent(new ProductBackInStockDomainEvent(Id, Name, Slug));

        Active = active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Replaces the full set of customers allowed to see/order this product. An empty set makes it public again.</summary>
    public void SetAllowedCustomers(IEnumerable<Guid> customerIds)
    {
        _allowedCustomerAccess.Clear();
        _allowedCustomerAccess.AddRange(customerIds.Distinct().Select(id => new ProductCustomerAccessEntry(id)));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Public products are visible to everyone; exclusive products only to their allowed customers.</summary>
    public bool HasAccess(Guid? customerId) =>
        !IsExclusive || (customerId is { } id && _allowedCustomerAccess.Any(e => e.CustomerId == id));

    /// <summary>Sets or clears a time-boxed promotional discount. Pass all three as null to clear an existing promotion.</summary>
    public void SetPromotion(decimal? discountPercentage, DateTime? startsAt, DateTime? endsAt)
    {
        if (discountPercentage is null && startsAt is null && endsAt is null)
        {
            DiscountPercentage = null;
            PromotionStartsAt = null;
            PromotionEndsAt = null;
            UpdatedAt = DateTime.UtcNow;
            return;
        }

        if (discountPercentage is null || discountPercentage <= 0 || discountPercentage >= 100)
            throw new DomainException("O desconto deve ser um percentual entre 1 e 99.");
        if (startsAt is null || endsAt is null)
            throw new DomainException("Informe o início e o fim do período da promoção.");
        if (endsAt <= startsAt)
            throw new DomainException("O fim da promoção deve ser depois do início.");

        DiscountPercentage = discountPercentage;
        PromotionStartsAt = startsAt;
        PromotionEndsAt = endsAt;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Replaces the full gallery (order is taken from the given sequence). An empty list clears it.</summary>
    public void SetImages(IEnumerable<string> urls)
    {
        _images.Clear();
        var order = 0;
        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;
            _images.Add(new ProductImage(url.Trim(), order++));
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
