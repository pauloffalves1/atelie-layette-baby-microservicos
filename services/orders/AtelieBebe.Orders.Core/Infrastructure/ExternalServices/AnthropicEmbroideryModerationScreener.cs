using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using AtelieBebe.Orders.Core.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Orders.Core.Infrastructure.ExternalServices;

/// <summary>Pre-screens embroidery text with Claude before an irreversible, physical item goes into
/// production. Never blocks the order on failure — degrades to "unflagged" (null) so a temporary API
/// problem never stops a checkout.</summary>
public sealed class AnthropicEmbroideryModerationScreener : IEmbroideryModerationScreener
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string[] KnownFlags = ["ofensivo", "dado_pessoal"];

    private readonly AnthropicClient _client;
    private readonly ILogger<AnthropicEmbroideryModerationScreener> _logger;

    public AnthropicEmbroideryModerationScreener(IConfiguration configuration, ILogger<AnthropicEmbroideryModerationScreener> logger)
    {
        _client = new AnthropicClient { ApiKey = configuration["Anthropic:ApiKey"] };
        _logger = logger;
    }

    public async Task<string?> ScreenAsync(string embroideryText, CancellationToken ct = default)
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
                System = "Você modera o texto que uma cliente escolheu para bordar numa peça de bebê "
                       + "(fralda/kit), sob encomenda e irreversível. Classifique como \"ofensivo\" "
                       + "(xingamento, ódio, conteúdo sexual/violento) ou \"dado_pessoal\" (expõe telefone, "
                       + "e-mail, endereço ou CPF de alguém — nomes de bebê/família são normais e não contam). "
                       + "Se nada disso se aplicar, retorne null.",
                Messages = [new() { Role = Role.User, Content = embroideryText }],
            }, ct);

            var json = response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text;
            var parsed = JsonSerializer.Deserialize<ParsedFlag>(json, CaseInsensitiveOptions)!;
            return parsed.Flag;
        }
        catch (AnthropicRateLimitException ex)
        {
            _logger.LogWarning(ex, "Rate limit na moderação de bordado — item segue sem sinalização automática");
            return null;
        }
        catch (Anthropic5xxException ex)
        {
            _logger.LogWarning(ex, "Erro do serviço da Anthropic na moderação de bordado — item segue sem sinalização automática");
            return null;
        }
        catch (AnthropicApiException ex)
        {
            _logger.LogError(ex, "Erro ao moderar texto de bordado — item segue sem sinalização automática");
            return null;
        }
        catch (AnthropicIOException ex)
        {
            _logger.LogWarning(ex, "Falha de rede ao moderar texto de bordado — item segue sem sinalização automática");
            return null;
        }
    }

    private sealed record ParsedFlag(string? Flag);
}
