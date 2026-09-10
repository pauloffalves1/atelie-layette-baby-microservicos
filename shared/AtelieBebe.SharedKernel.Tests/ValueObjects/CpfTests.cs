using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.SharedKernel.Tests.ValueObjects;

public class CpfTests
{
    [Theory]
    [InlineData("111.444.777-35")]
    [InlineData("11144477735")]
    public void Create_ValidCpf_NormalizesToDigitsOnly(string value)
    {
        var cpf = Cpf.Create(value);

        Assert.Equal("11144477735", cpf.Value);
    }

    [Fact]
    public void Create_AllDigitsEqual_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Cpf.Create("111.111.111-11"));
    }

    [Fact]
    public void Create_WrongCheckDigits_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Cpf.Create("111.444.777-36"));
    }

    [Fact]
    public void Create_WrongLength_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Cpf.Create("123456789"));
    }

    [Fact]
    public void Create_BlankValue_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Cpf.Create(""));
    }
}
