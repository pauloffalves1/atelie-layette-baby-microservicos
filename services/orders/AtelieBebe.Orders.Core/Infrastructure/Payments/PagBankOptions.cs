namespace AtelieBebe.Orders.Core.Infrastructure.Payments;

public sealed class PagBankOptions
{
    public const string SectionName = "PagBank";

    /// <summary>Token de integração do PagBank, gerado em minhaconta.pagbank.com.br (conta real) ou sandbox.pagseguro.uol.com.br (conta de teste). Blank in appsettings.json — set via dotnet user-secrets.</summary>
    public string Token { get; set; } = default!;

    /// <summary>True when Token was generated from a PagBank sandbox account — switches the API base address so no real charge is ever created while testing.</summary>
    public bool Sandbox { get; set; }
}
