using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Catalog.Core.Domain.Entities;

/// <summary>An admin-uploaded photo shown on the public /galeria page.</summary>
public sealed class GalleryImage : Entity, IAggregateRoot
{
    public string Url { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private GalleryImage() { } // EF Core

    private GalleryImage(Guid id, string url) : base(id)
    {
        Url = url;
        CreatedAt = DateTime.UtcNow;
    }

    public static GalleryImage Create(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("A URL da imagem é obrigatória.");

        return new GalleryImage(Guid.NewGuid(), url.Trim());
    }
}
