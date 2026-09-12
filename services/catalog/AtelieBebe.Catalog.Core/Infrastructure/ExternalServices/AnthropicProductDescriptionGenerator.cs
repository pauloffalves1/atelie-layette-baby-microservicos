using Anthropic;
using Anthropic.Models.Messages;
using AtelieBebe.Catalog.Core.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace AtelieBebe.Catalog.Core.Infrastructure.ExternalServices;

/// <summary>Generates a draft product description with Claude. Unlike the search/moderation
/// screeners, this is an explicit admin-triggered action — a failure is surfaced to the caller
/// instead of silently degrading, since there's no safe default text to fall back to.</summary>
public sealed class AnthropicProductDescriptionGenerator : IProductDescriptionGenerator
{
    private readonly AnthropicClient _client;

    public AnthropicProductDescriptionGenerator(IConfiguration configuration)
    {
        _client = new AnthropicClient { ApiKey = configuration["Anthropic:ApiKey"] };
    }

    public async Task<string> GenerateAsync(string name, string category, CancellationToken ct = default)
    {
        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = "claude-haiku-4-5",
            MaxTokens = 512,
            System = "Você escreve descrições de venda curtas (2 a 4 frases) para o catálogo de uma "
                   + "boutique de enxoval de bebê feito sob encomenda (fraldas de boca/ombro bordadas, kits). "
                   + "Tom caloroso e afetivo, sem exagero. Responda só com o texto da descrição, sem aspas "
                   + "e sem comentários adicionais.",
            Messages = [new() { Role = Role.User, Content = $"Produto: {name}\nCategoria: {category}" }],
        }, ct);

        return response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text.Trim();
    }
}
