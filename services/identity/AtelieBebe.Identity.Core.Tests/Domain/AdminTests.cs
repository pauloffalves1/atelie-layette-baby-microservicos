using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Identity.Core.Tests.Domain;

public class AdminTests
{
    private static Admin CreateAdmin() =>
        Admin.Create("Ateliê Admin", Email.Create("admin@ateliebebe.com.br"), "hash");

    [Fact]
    public void Create_MissingName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Admin.Create("", Email.Create("a@a.com"), "hash"));
    }

    [Fact]
    public void Create_MissingPasswordHash_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Admin.Create("Admin", Email.Create("a@a.com"), ""));
    }

    [Fact]
    public void Create_StartsWithTwoFactorDisabled()
    {
        var admin = CreateAdmin();

        Assert.False(admin.TwoFactorEnabled);
        Assert.Null(admin.TwoFactorSecret);
    }

    [Fact]
    public void EnableTwoFactor_MissingSecret_ThrowsDomainException()
    {
        var admin = CreateAdmin();

        Assert.Throws<DomainException>(() => admin.EnableTwoFactor(""));
    }

    [Fact]
    public void EnableTwoFactor_ValidSecret_TurnsTwoFactorOn()
    {
        var admin = CreateAdmin();

        admin.EnableTwoFactor("JBSWY3DPEHPK3PXP");

        Assert.True(admin.TwoFactorEnabled);
        Assert.Equal("JBSWY3DPEHPK3PXP", admin.TwoFactorSecret);
    }

    [Fact]
    public void DisableTwoFactor_ClearsSecretAndTurnsOff()
    {
        var admin = CreateAdmin();
        admin.EnableTwoFactor("JBSWY3DPEHPK3PXP");

        admin.DisableTwoFactor();

        Assert.False(admin.TwoFactorEnabled);
        Assert.Null(admin.TwoFactorSecret);
    }
}
