using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.Identity.Core.Application.Addresses;
using AtelieBebe.Identity.Core.Application.Auth;
using AtelieBebe.Identity.Core.Application.Customers;
using AtelieBebe.Identity.Core.Domain.Repositories;
using AtelieBebe.Identity.Core.Infrastructure;
using AtelieBebe.Identity.Core.Infrastructure.ExternalServices;
using AtelieBebe.Identity.Core.Infrastructure.Persistence;
using AtelieBebe.Identity.Core.Infrastructure.Persistence.Repositories;
using AtelieBebe.Identity.Core.Infrastructure.Security;
using AtelieBebe.SharedKernel.Messaging;
using AtelieBebe.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtelieBebe.Identity.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DomainEventsToOutboxInterceptor>();

        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=identity.db";
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(sp.GetRequiredService<DomainEventsToOutboxInterceptor>());
        });
        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AppUrlOptions>(configuration.GetSection(AppUrlOptions.SectionName));

        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();

        services.AddScoped<IAppUrlProvider, AppUrlProvider>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ITotpService, TotpService>();

        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<ICustomerAuthService, CustomerAuthService>();
        services.AddScoped<ICustomerAdminService, CustomerAdminService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();

        services.AddHttpClient<IOrdersServiceClient, OrdersServiceClient>((sp, client) =>
            client.BaseAddress = new Uri(configuration["Services:Orders"] ?? "http://orders:8080"));

        services.AddOutboxPublishing(configuration);

        return services;
    }
}
