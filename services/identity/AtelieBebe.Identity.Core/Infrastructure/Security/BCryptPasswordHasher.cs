using AtelieBebe.Identity.Core.Application.Abstractions;
using BC = BCrypt.Net.BCrypt;

namespace AtelieBebe.Identity.Core.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BC.HashPassword(password, workFactor: 11);

    public bool Verify(string password, string passwordHash) => BC.Verify(password, passwordHash);
}
