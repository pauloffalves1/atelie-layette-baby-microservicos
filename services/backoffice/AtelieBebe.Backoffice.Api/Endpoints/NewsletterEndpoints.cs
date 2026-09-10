using System.Globalization;
using System.Text;
using AtelieBebe.Backoffice.Core.Application.Newsletter;
using AtelieBebe.SharedKernel.Auth;

namespace AtelieBebe.Backoffice.Api.Endpoints;

public static class NewsletterEndpoints
{
    public static void MapNewsletterEndpoints(this WebApplication app)
    {
        app.MapPost("/api/newsletter/subscribe", async (SubscribeNewsletterRequest request, INewsletterService service, CancellationToken ct) =>
        {
            await service.SubscribeAsync(request.Email, ct);
            return Results.NoContent();
        }).WithTags("Newsletter");

        var adminGroup = app.MapGroup("/api/admin/newsletter").WithTags("Newsletter (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.Newsletter));

        adminGroup.MapGet("/", async (INewsletterService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        adminGroup.MapGet("/export", async (INewsletterService service, CancellationToken ct) =>
        {
            var subscribers = await service.ListAsync(ct);
            var culture = CultureInfo.GetCultureInfo("pt-BR");

            var sb = new StringBuilder();
            sb.AppendLine("E-mail;Inscrito em");
            foreach (var s in subscribers)
                sb.AppendLine($"{s.Email};{s.CreatedAt.ToString("dd/MM/yyyy HH:mm", culture)}");

            var fileName = $"newsletter-{DateTime.UtcNow:yyyy-MM-dd}.csv";
            return Results.File(new UTF8Encoding(true).GetBytes(sb.ToString()), "text/csv", fileName);
        });
    }
}
