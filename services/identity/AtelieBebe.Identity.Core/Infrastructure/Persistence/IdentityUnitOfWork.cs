using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.Identity.Core.Domain.Repositories;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence;

public sealed class IdentityUnitOfWork : IIdentityUnitOfWork
{
    private readonly IdentityDbContext _dbContext;

    public IdentityUnitOfWork(
        IdentityDbContext dbContext,
        IAdminRepository admins,
        ICustomerRepository customers,
        IPasswordResetTokenRepository passwordResetTokens,
        IEmailVerificationTokenRepository emailVerificationTokens,
        ICustomerAddressRepository customerAddresses)
    {
        _dbContext = dbContext;
        Admins = admins;
        Customers = customers;
        PasswordResetTokens = passwordResetTokens;
        EmailVerificationTokens = emailVerificationTokens;
        CustomerAddresses = customerAddresses;
    }

    public IAdminRepository Admins { get; }
    public ICustomerRepository Customers { get; }
    public IPasswordResetTokenRepository PasswordResetTokens { get; }
    public IEmailVerificationTokenRepository EmailVerificationTokens { get; }
    public ICustomerAddressRepository CustomerAddresses { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _dbContext.SaveChangesAsync(ct);
}
