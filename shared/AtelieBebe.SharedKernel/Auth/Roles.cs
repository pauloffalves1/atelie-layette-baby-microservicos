namespace AtelieBebe.SharedKernel.Auth;

/// <summary>JWT role claim values — shared so Identity (who signs) and every other service (who validates) never drift apart.</summary>
public static class Roles
{
    public const string Admin = "admin";
    public const string Customer = "customer";
}
