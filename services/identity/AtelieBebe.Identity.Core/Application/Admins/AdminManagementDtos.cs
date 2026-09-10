namespace AtelieBebe.Identity.Core.Application.Admins;

public sealed record AdminSummaryDto(
    Guid Id, string Name, string Email, bool TwoFactorEnabled, IReadOnlyList<string> Permissions, DateTime CreatedAt);

public sealed record CreateAdminRequest(string Name, string Email, string Password, IReadOnlyList<string> Permissions);

public sealed record UpdateAdminPermissionsRequest(IReadOnlyList<string> Permissions);
