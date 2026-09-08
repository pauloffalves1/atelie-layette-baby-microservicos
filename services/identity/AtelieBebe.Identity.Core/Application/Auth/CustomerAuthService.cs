using System.Security.Cryptography;
using System.Text;
using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;

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

    public CustomerAuthService(IIdentityUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IAppUrlProvider appUrlProvider, IOrdersServiceClient ordersServiceClient)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _appUrlProvider = appUrlProvider;
        _ordersServiceClient = ordersServiceClient;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterCustomerRequest request, CancellationToken ct = default)
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
        return new AuthResponse(token, customer.Id, customer.Name, customer.Email.Value);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var customer = await _unitOfWork.Customers.GetByEmailAsync(request.Email, ct);
        if (customer is null || customer.IsAnonymized || !_passwordHasher.Verify(request.Password, customer.PasswordHash))
            throw new UnauthorizedAppException("E-mail ou senha inválidos.");

        var token = _jwtTokenGenerator.GenerateCustomerToken(customer);
        return new AuthResponse(token, customer.Id, customer.Name, customer.Email.Value);
    }

    public async Task<CustomerProfileDto> GetProfileAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, ct)
            ?? throw new NotFoundException("Cliente", customerId);

        return new CustomerProfileDto(
            customer.Id, customer.Name, customer.Email.Value, customer.Phone, customer.Cpf?.Value,
            customer.AddressStreet, customer.AddressNumber, customer.AddressComplement,
            customer.AddressNeighborhood, customer.AddressCity, customer.AddressState, customer.AddressZipCode,
            customer.EmailVerified);
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        var customer = await _unitOfWork.Customers.GetByEmailAsync(email, ct);
        if (customer is null || customer.IsAnonymized) return;

        var rawToken = GenerateRawToken();
        var token = PasswordResetToken.Create(customer.Id, HashToken(rawToken), ResetTokenValidity);
        _unitOfWork.PasswordResetTokens.Add(token);

        var resetUrl = $"{_appUrlProvider.PublicUrl.TrimEnd('/')}/redefinir-senha?token={rawToken}";
        customer.RequestPasswordReset(resetUrl);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
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
    }

    public async Task DeleteAccountAsync(Guid customerId, string password, CancellationToken ct = default)
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
    }

    public async Task VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        var verificationToken = await _unitOfWork.EmailVerificationTokens.GetByTokenHashAsync(HashToken(token), ct);
        if (verificationToken is null || !verificationToken.IsValid)
            throw new UnauthorizedAppException("Link inválido ou expirado. Solicite um novo e-mail de verificação.");

        var customer = await _unitOfWork.Customers.GetByIdAsync(verificationToken.CustomerId, ct)
            ?? throw new UnauthorizedAppException("Link inválido ou expirado. Solicite um novo e-mail de verificação.");

        verificationToken.MarkUsed();
        customer.VerifyEmail();

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ResendEmailVerificationAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, ct);
        if (customer is null || customer.IsAnonymized || customer.EmailVerified) return;

        IssueEmailVerification(customer);
        await _unitOfWork.SaveChangesAsync(ct);
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
