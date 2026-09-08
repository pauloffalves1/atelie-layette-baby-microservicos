using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.Identity.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public PasswordResetTokenRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _dbContext.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public void Add(PasswordResetToken token) => _dbContext.PasswordResetTokens.Add(token);
}
