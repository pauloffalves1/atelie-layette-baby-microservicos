using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Identity.Core.Tests.Domain;

public class CustomerTests
{
    private static Customer RegisterCustomer() =>
        Customer.Register("Maria Silva", Email.Create("maria@exemplo.com"), Cpf.Create("11144477735"), "hash", "11999998888");

    [Fact]
    public void Register_MissingName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Customer.Register("", Email.Create("a@a.com"), Cpf.Create("11144477735"), "hash", "11999998888"));
    }

    [Fact]
    public void Register_MissingPhone_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Customer.Register("Maria", Email.Create("a@a.com"), Cpf.Create("11144477735"), "hash", ""));
    }

    [Fact]
    public void Register_MissingPasswordHash_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Customer.Register("Maria", Email.Create("a@a.com"), Cpf.Create("11144477735"), "", "11999998888"));
    }

    [Fact]
    public void Register_Valid_RaisesCustomerRegisteredDomainEvent()
    {
        var customer = RegisterCustomer();

        Assert.Contains(customer.DomainEvents, e => e.GetType().Name == "CustomerRegisteredDomainEvent");
    }

    [Fact]
    public void Register_Valid_StartsWithEmailUnverified()
    {
        var customer = RegisterCustomer();

        Assert.False(customer.EmailVerified);
    }

    [Fact]
    public void VerifyEmail_SetsEmailVerifiedToTrue()
    {
        var customer = RegisterCustomer();

        customer.VerifyEmail();

        Assert.True(customer.EmailVerified);
    }

    [Fact]
    public void UpdateDetails_ChangingEmail_ResetsEmailVerified()
    {
        var customer = RegisterCustomer();
        customer.VerifyEmail();

        customer.UpdateDetails("Maria Silva", Email.Create("novo-email@exemplo.com"), Cpf.Create("11144477735"), "11999998888");

        Assert.False(customer.EmailVerified);
    }

    [Fact]
    public void UpdateDetails_SameEmail_KeepsEmailVerified()
    {
        var customer = RegisterCustomer();
        customer.VerifyEmail();

        customer.UpdateDetails("Maria Silva", Email.Create("maria@exemplo.com"), Cpf.Create("11144477735"), "11999998888");

        Assert.True(customer.EmailVerified);
    }

    [Fact]
    public void Anonymize_ScrubsPersonalDataButKeepsId()
    {
        var customer = RegisterCustomer();
        var originalId = customer.Id;

        customer.Anonymize("hash-de-senha-inutilizavel");

        Assert.Equal(originalId, customer.Id);
        Assert.True(customer.IsAnonymized);
        Assert.Null(customer.Cpf);
        Assert.Null(customer.Phone);
        Assert.False(customer.EmailVerified);
        Assert.Equal($"cliente-removido-{originalId}@removido.local", customer.Email.Value);
    }

    [Fact]
    public void Anonymize_AlreadyAnonymized_IsIdempotent()
    {
        var customer = RegisterCustomer();
        customer.Anonymize("hash-1");
        var emailAfterFirstCall = customer.Email;

        customer.Anonymize("hash-2");

        Assert.Equal(emailAfterFirstCall, customer.Email);
    }

    [Fact]
    public void RequestPasswordReset_MissingUrl_ThrowsDomainException()
    {
        var customer = RegisterCustomer();

        Assert.Throws<DomainException>(() => customer.RequestPasswordReset(""));
    }

    [Fact]
    public void RequestPasswordReset_Valid_RaisesPasswordResetRequestedDomainEvent()
    {
        var customer = RegisterCustomer();

        customer.RequestPasswordReset("https://layettebaby.com.br/redefinir-senha?token=abc");

        Assert.Contains(customer.DomainEvents, e => e.GetType().Name == "PasswordResetRequestedDomainEvent");
    }
}
