using AtelieBebe.Identity.Core.Domain.Entities;

namespace AtelieBebe.Identity.Core.Domain.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    void Add(EmailVerificationToken token);
}
