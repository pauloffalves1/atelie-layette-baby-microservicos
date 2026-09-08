using AtelieBebe.Identity.Core.Domain.Entities;

namespace AtelieBebe.Identity.Core.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateCustomerToken(Customer customer);
    string GenerateAdminToken(Admin admin);
}
