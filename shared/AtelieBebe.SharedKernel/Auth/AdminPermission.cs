using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.SharedKernel.Auth;

/// <summary>
/// One flag per admin-facing feature area, shared by Identity (who grants/serializes these into the
/// JWT) and every other service (who each register one authorization policy per flag — see
/// <see cref="JwtAuthenticationExtensions"/>). An admin can hold any combination; <see cref="AdminManagement"/>
/// is itself just another flag — whoever holds it can create/remove admins and edit everyone's
/// permissions, including their own.
/// </summary>
[Flags]
public enum AdminPermission : long
{
    None = 0,
    Products = 1L << 0,
    Orders = 1L << 1,
    Coupons = 1L << 2,
    Reviews = 1L << 3,
    ContactMessages = 1L << 4,
    Newsletter = 1L << 5,
    Customers = 1L << 6,
    SiteContent = 1L << 7,
    Dashboard = 1L << 8,
    AdminManagement = 1L << 9,

    All = Products | Orders | Coupons | Reviews | ContactMessages | Newsletter | Customers | SiteContent | Dashboard | AdminManagement,
}

public static class AdminPermissionExtensions
{
    /// <summary>Every individual flag set in <paramref name="granted"/>, as its enum-member name — used both to expand a bitmask into one JWT claim per flag, and to serialize permissions for the admin UI.</summary>
    public static IReadOnlyList<string> ToPermissionStrings(this AdminPermission granted) =>
        Enum.GetValues<AdminPermission>()
            .Where(p => p is not (AdminPermission.None or AdminPermission.All) && granted.HasFlag(p))
            .Select(p => p.ToString())
            .ToArray();

    /// <summary>Combines permission-flag names (as sent by the admin-management UI) into a single bitmask. Throws <see cref="Exceptions.DomainException"/> for an unrecognized name.</summary>
    public static AdminPermission ParsePermissions(this IEnumerable<string> names)
    {
        var result = AdminPermission.None;
        foreach (var name in names)
        {
            if (!Enum.TryParse<AdminPermission>(name, out var permission) || permission is AdminPermission.None or AdminPermission.All)
                throw new DomainException($"Permissão desconhecida: '{name}'.");

            result |= permission;
        }
        return result;
    }
}
