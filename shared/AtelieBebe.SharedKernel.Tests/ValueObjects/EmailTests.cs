using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.SharedKernel.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_BlankValue_ThrowsDomainException(string? value)
    {
        Assert.Throws<DomainException>(() => Email.Create(value!));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("no-at-sign.com")]
    public void Create_InvalidFormat_ThrowsDomainException(string value)
    {
        Assert.Throws<DomainException>(() => Email.Create(value));
    }

    [Fact]
    public void Create_MixedCaseWithSurroundingWhitespace_NormalizesToLowercaseTrimmed()
    {
        var email = Email.Create("  Cliente@AtelieBebe.com.br  ");

        Assert.Equal("cliente@atelieBebe.com.br".ToLowerInvariant(), email.Value);
    }

    [Fact]
    public void Equality_SameAddressDifferentCase_AreEqual()
    {
        Assert.Equal(Email.Create("Ana@Exemplo.com"), Email.Create("ana@exemplo.com"));
    }
}
