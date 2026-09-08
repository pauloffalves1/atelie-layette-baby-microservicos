using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.Backoffice.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Persistence.Repositories;

public sealed class ContactMessageRepository : IContactMessageRepository
{
    private readonly BackofficeDbContext _dbContext;

    public ContactMessageRepository(BackofficeDbContext dbContext) => _dbContext = dbContext;

    public void Add(ContactMessage message) => _dbContext.ContactMessages.Add(message);

    public async Task<(IReadOnlyList<ContactMessage> Items, int TotalItems)> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.ContactMessages.OrderByDescending(m => m.CreatedAt);

        var totalItems = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalItems);
    }
}
