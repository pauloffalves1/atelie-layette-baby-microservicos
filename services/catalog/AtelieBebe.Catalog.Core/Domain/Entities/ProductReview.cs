using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Catalog.Core.Domain.Entities;

/// <summary>A star rating (1-5) plus an optional comment left by a customer who bought the product.</summary>
public sealed class ProductReview : Entity, IAggregateRoot
{
    public Guid ProductId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public string? PhotoUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Pending admin approval until set — new reviews never show on the storefront right away.</summary>
    public bool Approved { get; private set; }

    private ProductReview() { } // EF Core

    private ProductReview(Guid id, Guid productId, Guid customerId, string customerName, int rating, string? comment, string? photoUrl) : base(id)
    {
        ProductId = productId;
        CustomerId = customerId;
        CustomerName = customerName;
        Rating = rating;
        Comment = comment;
        PhotoUrl = photoUrl;
        CreatedAt = DateTime.UtcNow;
        Approved = false;
    }

    public void Approve() => Approved = true;

    public static ProductReview Create(Guid productId, Guid customerId, string customerName, int rating, string? comment, string? photoUrl = null)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Produto inválido.");
        if (customerId == Guid.Empty)
            throw new DomainException("Cliente inválido.");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Nome do cliente é obrigatório.");
        if (rating is < 1 or > 5)
            throw new DomainException("A nota deve ser entre 1 e 5.");

        return new ProductReview(
            Guid.NewGuid(), productId, customerId, customerName.Trim(), rating,
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim());
    }
}
