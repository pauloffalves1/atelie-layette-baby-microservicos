using AtelieBebe.Identity.Core.Domain.Entities;

namespace AtelieBebe.Identity.Core.Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    void Add(PasswordResetToken token);
}
