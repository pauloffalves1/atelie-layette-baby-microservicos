using Anthropic;
using Anthropic.Models.Messages;
using AtelieBebe.Backoffice.Core.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace AtelieBebe.Backoffice.Core.Infrastructure.ExternalServices;

/// <summary>Drafts a reply to a contact message with Claude. Explicit admin-triggered action — a
/// failure is surfaced to the caller instead of silently degrading, since there's no safe default text.</summary>
public sealed class AnthropicContactReplyDrafter : IContactReplyDrafter
{
    private readonly AnthropicClient _client;

    public AnthropicContactReplyDrafter(IConfiguration configuration)
    {
        _client = new AnthropicClient { ApiKey = configuration["Anthropic:ApiKey"] };
    }

    public async Task<string> DraftAsync(string customerName, string message, CancellationToken ct = default)
    {
        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = "claude-haiku-4-5",
            MaxTokens = 768,
            System = "Você escreve rascunhos de resposta para mensagens de contato de uma boutique de "
                   + "enxoval de bebê feito sob encomenda. Tom caloroso, cordial e objetivo. Responda só "
                   + "com o texto da resposta (pronta para a administradora revisar e enviar), sem aspas, "
                   + "sem saudação de e-mail formal exagerada, sem comentários adicionais.",
            Messages = [new() { Role = Role.User, Content = $"Cliente: {customerName}\nMensagem recebida: {message}" }],
        }, ct);

        return response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text.Trim();
    }
}
