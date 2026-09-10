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
}
