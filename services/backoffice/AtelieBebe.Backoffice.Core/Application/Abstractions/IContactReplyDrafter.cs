namespace AtelieBebe.Backoffice.Core.Application.Abstractions;

/// <summary>Drafts a reply to a contact message, for the admin to review and edit before sending it manually.</summary>
public interface IContactReplyDrafter
{
    Task<string> DraftAsync(string customerName, string message, CancellationToken ct = default);
}
