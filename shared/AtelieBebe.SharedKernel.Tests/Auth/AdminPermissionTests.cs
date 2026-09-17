using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.SharedKernel.Tests.Auth;

public class AdminPermissionTests
{
    [Fact]
    public void ToPermissionStrings_ExpandsEachGrantedFlag()
    {
        var granted = AdminPermission.Products | AdminPermission.Orders;

        var names = granted.ToPermissionStrings();

        Assert.Equal(new[] { "Products", "Orders" }, names);
    }

    [Fact]
    public void ToPermissionStrings_None_ReturnsEmpty()
    {
        Assert.Empty(AdminPermission.None.ToPermissionStrings());
    }

    [Fact]
    public void ToPermissionStrings_All_DoesNotIncludeTheAggregateFlagItself()
    {
        var names = AdminPermission.All.ToPermissionStrings();

        Assert.DoesNotContain("All", names);
        Assert.Contains("AdminManagement", names);
    }

    [Fact]
    public void ParsePermissions_ValidNames_CombinesFlags()
    {
        var permissions = new[] { "Products", "Orders" }.ParsePermissions();

        Assert.Equal(AdminPermission.Products | AdminPermission.Orders, permissions);
    }

    [Fact]
    public void ParsePermissions_EmptyList_ReturnsNone()
    {
        Assert.Equal(AdminPermission.None, Array.Empty<string>().ParsePermissions());
    }

    [Fact]
    public void ParsePermissions_UnknownName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new[] { "NotARealPermission" }.ParsePermissions());
    }

    [Theory]
    [InlineData("None")]
    [InlineData("All")]
    public void ParsePermissions_RejectsAggregateValues(string name)
    {
        Assert.Throws<DomainException>(() => new[] { name }.ParsePermissions());
    }

    /// <summary>RF40: the test screen is never handed out by "grant everything" — the seeded general
    /// admin included. It has to be ticked on purpose for the account that tests payments.</summary>
    [Fact]
    public void All_DoesNotIncludeTesting()
    {
        Assert.False(AdminPermission.All.HasFlag(AdminPermission.Testing));
        Assert.DoesNotContain("Testing", AdminPermission.All.ToPermissionStrings());
    }

    [Fact]
    public void ParsePermissions_AcceptsTesting()
    {
        Assert.Equal(AdminPermission.Testing, new[] { "Testing" }.ParsePermissions());
    }
}
