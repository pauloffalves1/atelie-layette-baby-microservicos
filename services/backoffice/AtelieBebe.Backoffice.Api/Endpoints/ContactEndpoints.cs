using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Application.Contact;
using AtelieBebe.SharedKernel.Auth;

namespace AtelieBebe.Backoffice.Api.Endpoints;

public static class ContactEndpoints
{
    public static void MapContactEndpoints(this WebApplication app)
    {
        app.MapPost("/api/contact", async (SubmitContactRequest request, IContactService service, CancellationToken ct) =>
        {
            await service.SubmitAsync(request, ct);
            return Results.NoContent();
        }).WithTags("Contato");

        app.MapGet("/api/admin/contact-messages", async (IContactService service, CancellationToken ct, int page = 1, int pageSize = 20) =>
            Results.Ok(await service.ListAsync(page, pageSize, ct)))
            .WithTags("Contato (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.ContactMessages));

        app.MapPost("/api/admin/contact-messages/suggest-reply", async (SuggestContactReplyRequest request, IContactReplyDrafter drafter, CancellationToken ct) =>
            Results.Ok(new SuggestContactReplyResponse(await drafter.DraftAsync(request.CustomerName, request.Message, ct))))
            .WithTags("Contato (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.ContactMessages));
    }
}
