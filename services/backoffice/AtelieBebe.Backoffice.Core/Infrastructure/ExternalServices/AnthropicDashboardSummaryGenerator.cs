using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Application.Dashboard;
using Microsoft.Extensions.Configuration;

namespace AtelieBebe.Backoffice.Core.Infrastructure.ExternalServices;

/// <summary>Summarizes the dashboard's numbers with Claude. Explicit admin-triggered action — a
/// failure is surfaced to the caller instead of silently degrading, since there's no safe default text.</summary>
public sealed class AnthropicDashboardSummaryGenerator : IDashboardSummaryGenerator
{
    private readonly AnthropicClient _client;

    public AnthropicDashboardSummaryGenerator(IConfiguration configuration)
    {
        _client = new AnthropicClient { ApiKey = configuration["Anthropic:ApiKey"] };
    }

    public async Task<string> SummarizeAsync(DashboardDto dashboard, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Pedidos totais: {dashboard.TotalOrders}, em aberto: {dashboard.OpenOrders}");
        sb.AppendLine($"Receita total: R$ {dashboard.RevenueTotal:F2}, receita do mês: R$ {dashboard.RevenueThisMonth:F2}");
        sb.AppendLine($"Ticket médio: R$ {dashboard.AverageOrderValue:F2}");
        sb.AppendLine($"Produtos cadastrados: {dashboard.TotalProducts}, clientes: {dashboard.TotalCustomers}");
        sb.AppendLine("Pedidos por status: " + string.Join(", ", dashboard.OrdersByStatus.Select(s => $"{s.Status}={s.Count}")));
        sb.AppendLine("Produtos mais vendidos: " + string.Join(", ", dashboard.TopProducts.Select(p => $"{p.ProductName} ({p.QuantitySold} un., R$ {p.Revenue:F2})")));
        sb.AppendLine("Vendas últimos 30 dias: " + string.Join(", ", dashboard.SalesLast30Days.Select(d => $"{d.Date:dd/MM}: R$ {d.Revenue:F2} ({d.OrderCount} pedidos)")));

        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = "claude-haiku-4-5",
            MaxTokens = 768,
            System = "Você resume os números do painel de uma boutique de enxoval de bebê para a dona do "
                   + "negócio, que não tem tempo de analisar gráfico. Escreva 3 a 5 frases em português, tom "
                   + "direto e prático: destaque tendências (crescimento/queda), o produto/categoria em "
                   + "destaque, e qualquer ponto que mereça atenção (ex: muitos pedidos parados em um status). "
                   + "Responda só com o texto do resumo, sem título, sem comentários adicionais.",
            Messages = [new() { Role = Role.User, Content = sb.ToString() }],
        }, ct);

        return response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text.Trim();
    }
}
