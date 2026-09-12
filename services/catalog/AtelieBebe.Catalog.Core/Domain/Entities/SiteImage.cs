using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Catalog.Core.Domain.Entities;

/// <summary>
/// A named image slot for static site content (e.g. the homepage hero photo) that the admin
/// can replace without a code deploy. Distinct from Product.ImageUrl, which belongs to a
/// specific catalog item.
/// </summary>
public sealed class SiteImage : Entity, IAggregateRoot
{
    public string Key { get; private set; } = default!;
    public string Url { get; private set; } = default!;

    /// <summary>Position within its key's group, ascending. Most keys hold a single image (order
    /// irrelevant), but a key can hold several — e.g. "home-hero" renders as a carousel — in which
    /// case this controls playback order.</summary>
    public int SortOrder { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SiteImage() { } // EF Core

    private SiteImage(Guid id, string key, string url, int sortOrder) : base(id)
    {
        Key = key;
        Url = url;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public static SiteImage Create(string key, string url, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("A chave da imagem é obrigatória.");
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("A URL da imagem é obrigatória.");

        return new SiteImage(Guid.NewGuid(), key.Trim(), url.Trim(), sortOrder);
    }

    public void UpdateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("A URL da imagem é obrigatória.");

        Url = url.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }
}
