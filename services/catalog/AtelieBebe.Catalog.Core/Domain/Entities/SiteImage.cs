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
    public DateTime UpdatedAt { get; private set; }

    private SiteImage() { } // EF Core

    private SiteImage(Guid id, string key, string url) : base(id)
    {
        Key = key;
        Url = url;
        UpdatedAt = DateTime.UtcNow;
    }

    public static SiteImage Create(string key, string url)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("A chave da imagem é obrigatória.");
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("A URL da imagem é obrigatória.");

        return new SiteImage(Guid.NewGuid(), key.Trim(), url.Trim());
    }

    public void UpdateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("A URL da imagem é obrigatória.");

        Url = url.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
