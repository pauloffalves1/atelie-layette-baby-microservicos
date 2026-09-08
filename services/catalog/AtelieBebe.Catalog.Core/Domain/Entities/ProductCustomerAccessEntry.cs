namespace AtelieBebe.Catalog.Core.Domain.Entities;

/// <summary>
/// One grant of access to an exclusive <see cref="Product"/>. Owned by <see cref="Product"/> — never
/// queried or referenced on its own, it only exists so EF Core can map the customer-access list to its
/// own join table (ProductCustomerAccess) instead of a JSON column.
/// </summary>
public sealed class ProductCustomerAccessEntry
{
    public Guid CustomerId { get; private set; }

    private ProductCustomerAccessEntry() { } // EF Core

    public ProductCustomerAccessEntry(Guid customerId) => CustomerId = customerId;
}
