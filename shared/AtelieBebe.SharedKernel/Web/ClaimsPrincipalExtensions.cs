using System.Security.Claims;

namespace AtelieBebe.SharedKernel.Web;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token sem identificador de usuário.");
        return Guid.Parse(value);
    }

    /// <summary>Like <see cref="GetUserId"/>, but returns null instead of throwing when there is no authenticated user.</summary>
    public static Guid? GetUserIdOrNull(this ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true ? principal.GetUserId() : null;

    public static string GetName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Name) ?? "Desconhecido";

    /// <summary>
    /// True when the caller is a customer an admin approved as a test user (RF40): she is the only
    /// one who can see test products, and everything she buys is recorded as a test purchase.
    /// False for anonymous visitors and for admins — an admin token never carries this claim.
    /// </summary>
    public static bool IsTestUser(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(Auth.JwtAuthenticationExtensions.TestUserClaimType) == "true";
}
