using AtelieBebe.Identity.Core.Domain.Repositories;

namespace AtelieBebe.Identity.Core.Application.Abstractions;

/// <summary>
/// Identity service's own unit of work — only the repositories this bounded context owns
/// (Admin/Customer/tokens). The monolith's IUnitOfWork bundled every aggregate's repository
/// together; each microservice gets its own much smaller one instead.
/// </summary>
public interface IIdentityUnitOfWork
{
    IAdminRepository Admins { get; }
    ICustomerRepository Customers { get; }
    IPasswordResetTokenRepository PasswordResetTokens { get; }
    IEmailVerificationTokenRepository EmailVerificationTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
