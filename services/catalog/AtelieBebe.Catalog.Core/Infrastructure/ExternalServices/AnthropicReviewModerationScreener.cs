using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using AtelieBebe.Catalog.Core.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Catalog.Core.Infrastructure.ExternalServices;

/// <summary>Pre-screens review comments with Claude. Never blocks a review on failure — degrades to
/// "unflagged" (null) so a temporary API problem never stops a customer from posting a review.</summary>
public sealed class AnthropicReviewModerationScreener : IReviewModerationScreener
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string[] KnownFlags = ["spam", "agressivo", "dado_pessoal"];

    private readonly AnthropicClient _client;
    private readonly ILogger<AnthropicReviewModerationScreener> _logger;

    public AnthropicReviewModerationScreener(IConfiguration configuration, ILogger<AnthropicReviewModerationScreener> logger)
    {
        _client = new AnthropicClient { ApiKey = configuration["Anthropic:ApiKey"] };
        _logger = logger;
    }

    public async Task<string?> ScreenAsync(string comment, CancellationToken ct = default)
    {
        try
        {
            var schema = new Dictionary<string, JsonElement>
            {
                ["type"] = JsonSerializer.SerializeToElement("object"),
                ["properties"] = JsonSerializer.SerializeToElement(new
                {
                    flag = new
                    {
                        anyOf = new object[]
                        {
                            new { type = "string", @enum = KnownFlags },
                            new { type = "null" },
                        },
                    },
                }),
                ["required"] = JsonSerializer.SerializeToElement(new[] { "flag" }),
                ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
            };

            var response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = "claude-haiku-4-5",
                MaxTokens = 512,
                OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = schema } },
                System = "Você modera comentários de avaliação de produto de uma loja de enxoval de bebê. "
                       + "Classifique o comentário como \"spam\" (propaganda/link/sem relação com o produto), "
                       + "\"agressivo\" (ofensivo, xingamento, ameaça) ou \"dado_pessoal\" (expõe telefone, e-mail, "
                       + "endereço ou CPF de alguém). Se nada disso se aplicar, retorne null.",
                Messages = [new() { Role = Role.User, Content = comment }],
            }, ct);

            var json = response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text;
            var parsed = JsonSerializer.Deserialize<ParsedFlag>(json, CaseInsensitiveOptions)!;
            return parsed.Flag;
        }
        catch (AnthropicRateLimitException ex)
        {
            _logger.LogWarning(ex, "Rate limit na pré-triagem de avaliação — avaliação segue sem sinalização automática");
            return null;
        }
        catch (Anthropic5xxException ex)
        {
            _logger.LogWarning(ex, "Erro do serviço da Anthropic na pré-triagem de avaliação — avaliação segue sem sinalização automática");
            return null;
        }
        catch (AnthropicApiException ex)
        {
            _logger.LogError(ex, "Erro ao pré-triar avaliação — avaliação segue sem sinalização automática");
            return null;
        }
        catch (AnthropicIOException ex)
        {
            _logger.LogWarning(ex, "Falha de rede ao pré-triar avaliação — avaliação segue sem sinalização automática");
            return null;
        }
    }

    private sealed record ParsedFlag(string? Flag);
}
