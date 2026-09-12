using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Application.Products;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Catalog.Core.Infrastructure.ExternalServices;

/// <summary>Translates a storefront search query into structured filters using Claude. Degrades to a
/// keywords-only filter (rather than failing the whole search) if the Anthropic API call fails.</summary>
public sealed class AnthropicSemanticSearchTranslator : ISemanticSearchTranslator
{
    // Claude returns camelCase field names per the JSON schema; ParsedFilters uses PascalCase
    // C# conventions, so case-insensitive matching is required or every field deserializes to
    // its default (null/false) with no error.
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly AnthropicClient _client;
    private readonly ILogger<AnthropicSemanticSearchTranslator> _logger;

    public AnthropicSemanticSearchTranslator(IConfiguration configuration, ILogger<AnthropicSemanticSearchTranslator> logger)
    {
        _client = new AnthropicClient { ApiKey = configuration["Anthropic:ApiKey"] };
        _logger = logger;
    }

    public async Task<ProductSearchFilters> TranslateAsync(string naturalLanguageQuery, IReadOnlyList<string> knownCategories, CancellationToken ct = default)
    {
        try
        {
            var schema = new Dictionary<string, JsonElement>
            {
                ["type"] = JsonSerializer.SerializeToElement("object"),
                ["properties"] = JsonSerializer.SerializeToElement(new
                {
                    category = new
                    {
                        anyOf = new object[]
                        {
                            new { type = "string", @enum = knownCategories.ToArray() },
                            new { type = "null" },
                        },
                    },
                    minPrice = new { type = new[] { "number", "null" } },
                    maxPrice = new { type = new[] { "number", "null" } },
                    keywords = new { type = new[] { "string", "null" }, description = "Palavras-chave livres para casar com nome/descrição do produto" },
                    onlyOnPromotion = new { type = "boolean" },
                }),
                ["required"] = JsonSerializer.SerializeToElement(new[] { "category", "minPrice", "maxPrice", "keywords", "onlyOnPromotion" }),
                ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
            };

            var response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = "claude-haiku-4-5",
                MaxTokens = 1024,
                OutputConfig = new OutputConfig
                {
                    Format = new JsonOutputFormat { Schema = schema },
                },
                System = "Você traduz buscas em linguagem natural de uma loja de enxoval de bebê feito sob "
                       + $"encomenda em filtros estruturados. Categorias existentes: {string.Join(", ", knownCategories)}. "
                       + "Se o(a) cliente não mencionar preço, categoria ou promoção, use null/false.",
                Messages = [new() { Role = Role.User, Content = naturalLanguageQuery }],
            }, ct);

            var json = response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text;
            var parsed = JsonSerializer.Deserialize<ParsedFilters>(json, CaseInsensitiveOptions)!;
            return new ProductSearchFilters(parsed.Category, parsed.MinPrice, parsed.MaxPrice, parsed.Keywords, parsed.OnlyOnPromotion);
        }
        catch (AnthropicRateLimitException ex)
        {
            _logger.LogWarning(ex, "Rate limit na tradução de busca semântica — usando fallback de palavras-chave");
            return Fallback(naturalLanguageQuery);
        }
        catch (Anthropic5xxException ex)
        {
            _logger.LogWarning(ex, "Erro do serviço da Anthropic na tradução de busca semântica — usando fallback de palavras-chave");
            return Fallback(naturalLanguageQuery);
        }
        catch (AnthropicApiException ex)
        {
            _logger.LogError(ex, "Erro ao traduzir busca semântica — usando fallback de palavras-chave");
            return Fallback(naturalLanguageQuery);
        }
        catch (AnthropicIOException ex)
        {
            _logger.LogWarning(ex, "Falha de rede ao chamar a Anthropic na busca semântica — usando fallback de palavras-chave");
            return Fallback(naturalLanguageQuery);
        }
    }

    private static ProductSearchFilters Fallback(string naturalLanguageQuery) =>
        new(Category: null, MinPrice: null, MaxPrice: null, Keywords: naturalLanguageQuery, OnlyOnPromotion: false);

    private sealed record ParsedFilters(string? Category, decimal? MinPrice, decimal? MaxPrice, string? Keywords, bool OnlyOnPromotion);
}
