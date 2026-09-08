using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.Identity.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence.Repositories;

public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public EmailVerificationTokenRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _dbContext.EmailVerificationTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public void Add(EmailVerificationToken token) => _dbContext.EmailVerificationTokens.Add(token);
}
