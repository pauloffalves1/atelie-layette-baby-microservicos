using System.Security.Cryptography;
using System.Text;
using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Identity.Core.Application.Auth;

public sealed class CustomerAuthService : ICustomerAuthService
{
    private static readonly TimeSpan ResetTokenValidity = TimeSpan.FromHours(1);
    private static readonly TimeSpan VerificationTokenValidity = TimeSpan.FromHours(24);

    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAppUrlProvider _appUrlProvider;
    private readonly IOrdersServiceClient _ordersServiceClient;
    private readonly ILogger<CustomerAuthService> _logger;

    public CustomerAuthService(IIdentityUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IAppUrlProvider appUrlProvider, IOrdersServiceClient ordersServiceClient, ILogger<CustomerAuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _appUrlProvider = appUrlProvider;
        _ordersServiceClient = ordersServiceClient;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterCustomerRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {Email}", nameof(RegisterAsync), request.Email);
        try
        {
            if (await _unitOfWork.Customers.EmailExistsAsync(request.Email, ct))
                throw new ConflictException("Já existe uma conta com este e-mail.");

            var cpf = Cpf.Create(request.Cpf);
            if (await _unitOfWork.Customers.CpfExistsAsync(cpf.Value, ct))
                throw new ConflictException("Já existe uma conta com este CPF.");

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                throw new ConflictException("A senha deve ter pelo menos 6 caracteres.");

            var customer = Customer.Register(
                request.Name,
                Email.Create(request.Email),
                cpf,
                _passwordHasher.Hash(request.Password),
                request.Phone,
                request.AddressStreet,
                request.AddressNumber,
                request.AddressComplement,
                request.AddressNeighborhood,
                request.AddressCity,
                request.AddressState,
                request.AddressZipCode);

            _unitOfWork.Customers.Add(customer);
            IssueEmailVerification(customer);
            await _unitOfWork.SaveChangesAsync(ct);

            var token = _jwtTokenGenerator.GenerateCustomerToken(customer);
            _logger.LogInformation("Saindo de {Method}", nameof(RegisterAsync));
            return new AuthResponse(token, customer.Id, customer.Name, customer.Email.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(RegisterAsync));
            throw;
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {Email}", nameof(LoginAsync), request.Email);
        try
        {
            var customer = await _unitOfWork.Customers.GetByEmailAsync(request.Email, ct);
            if (customer is null || customer.IsAnonymized || !_passwordHasher.Verify(request.Password, customer.PasswordHash))
                throw new UnauthorizedAppException("E-mail ou senha inválidos.");

            var token = _jwtTokenGenerator.GenerateCustomerToken(customer);
            _logger.LogInformation("Saindo de {Method}", nameof(LoginAsync));
            return new AuthResponse(token, customer.Id, customer.Name, customer.Email.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(LoginAsync));
            throw;
        }
    }

    public async Task<CustomerProfileDto> GetProfileAsync(Guid customerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}", nameof(GetProfileAsync), customerId);
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, ct)
                ?? throw new NotFoundException("Cliente", customerId);

            _logger.LogInformation("Saindo de {Method}", nameof(GetProfileAsync));
            return new CustomerProfileDto(
                customer.Id, customer.Name, customer.Email.Value, customer.Phone, customer.Cpf?.Value,
                customer.AddressStreet, customer.AddressNumber, customer.AddressComplement,
                customer.AddressNeighborhood, customer.AddressCity, customer.AddressState, customer.AddressZipCode,
                customer.EmailVerified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetProfileAsync));
            throw;
        }
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {Email}", nameof(RequestPasswordResetAsync), email);
        try
        {
            var customer = await _unitOfWork.Customers.GetByEmailAsync(email, ct);
            if (customer is null || customer.IsAnonymized)
            {
                _logger.LogInformation("Saindo de {Method}", nameof(RequestPasswordResetAsync));
                return;
            }

            var rawToken = GenerateRawToken();
            var token = PasswordResetToken.Create(customer.Id, HashToken(rawToken), ResetTokenValidity);
            _unitOfWork.PasswordResetTokens.Add(token);

            var resetUrl = $"{_appUrlProvider.PublicUrl.TrimEnd('/')}/redefinir-senha?token={rawToken}";
            customer.RequestPasswordReset(resetUrl);

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(RequestPasswordResetAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(RequestPasswordResetAsync));
            throw;
        }
    }

    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ResetPasswordAsync));
        try
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                throw new ConflictException("A senha deve ter pelo menos 6 caracteres.");

            var resetToken = await _unitOfWork.PasswordResetTokens.GetByTokenHashAsync(HashToken(token), ct);
            if (resetToken is null || !resetToken.IsValid)
                throw new UnauthorizedAppException("Link inválido ou expirado. Solicite uma nova redefinição de senha.");

            var customer = await _unitOfWork.Customers.GetByIdAsync(resetToken.CustomerId, ct)
                ?? throw new UnauthorizedAppException("Link inválido ou expirado. Solicite uma nova redefinição de senha.");

            resetToken.MarkUsed();
            customer.UpdatePassword(_passwordHasher.Hash(newPassword));

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(ResetPasswordAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ResetPasswordAsync));
            throw;
        }
    }

    public async Task DeleteAccountAsync(Guid customerId, string password, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}", nameof(DeleteAccountAsync), customerId);
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, ct)
                ?? throw new NotFoundException("Cliente", customerId);

            if (customer.IsAnonymized || !_passwordHasher.Verify(password, customer.PasswordHash))
                throw new UnauthorizedAppException("Senha incorreta.");

            // Orders keep their own snapshot of the customer's data at purchase time, so deleting the
            // account never loses order history — only whether the *account itself* survives depends
            // on there being anything to retain it for (a legal/fiscal reason to keep the profile row).
            // Orders lives in its own service/database now, so this is the one place Identity makes a
            // synchronous internal HTTP call instead of querying a local repository.
            var hasOrders = await _ordersServiceClient.CustomerHasOrdersAsync(customerId, ct);
            if (!hasOrders)
                _unitOfWork.Customers.Remove(customer);
            else
                customer.Anonymize(_passwordHasher.Hash(Guid.NewGuid().ToString("N")));

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(DeleteAccountAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(DeleteAccountAsync));
            throw;
        }
    }

    public async Task VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(VerifyEmailAsync));
        try
        {
            var verificationToken = await _unitOfWork.EmailVerificationTokens.GetByTokenHashAsync(HashToken(token), ct);
            if (verificationToken is null || !verificationToken.IsValid)
                throw new UnauthorizedAppException("Link inválido ou expirado. Solicite um novo e-mail de verificação.");

            var customer = await _unitOfWork.Customers.GetByIdAsync(verificationToken.CustomerId, ct)
                ?? throw new UnauthorizedAppException("Link inválido ou expirado. Solicite um novo e-mail de verificação.");

            verificationToken.MarkUsed();
            customer.VerifyEmail();

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(VerifyEmailAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(VerifyEmailAsync));
            throw;
        }
    }

    public async Task ResendEmailVerificationAsync(Guid customerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}", nameof(ResendEmailVerificationAsync), customerId);
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, ct);
            if (customer is null || customer.IsAnonymized || customer.EmailVerified)
            {
                _logger.LogInformation("Saindo de {Method}", nameof(ResendEmailVerificationAsync));
                return;
            }

            IssueEmailVerification(customer);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(ResendEmailVerificationAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ResendEmailVerificationAsync));
            throw;
        }
    }

    private void IssueEmailVerification(Customer customer)
    {
        var rawToken = GenerateRawToken();
        var token = EmailVerificationToken.Create(customer.Id, HashToken(rawToken), VerificationTokenValidity);
        _unitOfWork.EmailVerificationTokens.Add(token);

        var verificationUrl = $"{_appUrlProvider.PublicUrl.TrimEnd('/')}/verificar-email?token={rawToken}";
        customer.RequestEmailVerification(verificationUrl);
    }

    private static string GenerateRawToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string rawToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
