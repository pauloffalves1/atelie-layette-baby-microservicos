using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Backoffice.Core.Tests.Domain;

public class ContactMessageTests
{
    [Fact]
    public void Create_MissingName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ContactMessage.Create("", Email.Create("a@a.com"), "11999998888", "Olá, gostaria de um orçamento."));
    }

    [Fact]
    public void Create_MissingPhone_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ContactMessage.Create("Maria", Email.Create("a@a.com"), "", "Olá, gostaria de um orçamento."));
    }

    [Fact]
    public void Create_MissingMessage_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ContactMessage.Create("Maria", Email.Create("a@a.com"), "11999998888", ""));
    }

    [Fact]
    public void Create_Valid_RaisesContactMessageReceivedDomainEvent()
    {
        var message = ContactMessage.Create("Maria", Email.Create("maria@exemplo.com"), "11999998888", "Olá, gostaria de um orçamento.");

        Assert.Contains(message.DomainEvents, e => e.GetType().Name == "ContactMessageReceivedDomainEvent");
    }

    [Fact]
    public void Create_TrimsNameAndPhone()
    {
        var message = ContactMessage.Create("  Maria  ", Email.Create("maria@exemplo.com"), "  11999998888  ", "Olá");

        Assert.Equal("Maria", message.Name);
        Assert.Equal("11999998888", message.Phone);
    }
}
