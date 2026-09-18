using System.Text.Json;

namespace AtelieBebe.Orders.Core.Infrastructure.Payments;

/// <summary>
/// Turns a PagBank error body into something worth showing the customer — but only for the refusals
/// that are permanent and actionable. Everything else keeps the generic "try again in a moment"
/// message, which is honest for a timeout or an outage and useless for a rule that will refuse the
/// same request forever.
/// </summary>
public static class PagBankErrorMessages
{
    /// <summary>
    /// A customer-facing message for a refusal the customer can actually do something about, or null
    /// when the failure is transient/unknown and the caller should fall back to the generic message.
    /// </summary>
    public static string? ForCustomer(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;

        try
        {
            var json = JsonDocument.Parse(responseBody).RootElement;
            if (!json.TryGetProperty("error_messages", out var errors) || errors.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var error in errors.EnumerateArray())
            {
                var code = error.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
                var parameter = error.TryGetProperty("parameter_name", out var paramEl) ? paramEl.GetString() : null;

                // Seen in production: PagBank refuses any purchase whose buyer e-mail is the store's
                // own account e-mail ("buyer email must not be equals to merchant email"). Retrying
                // never helps — only a different e-mail does.
                if (code == "40002" && parameter == "customer.email")
                {
                    return "Este e-mail não pode ser usado para pagar, porque é o mesmo e-mail da conta do ateliê " +
                           "no PagBank. Finalize a compra com outro e-mail ou fale conosco pelo WhatsApp.";
                }
            }

            return null;
        }
        catch (JsonException)
        {
            // Not the shape we know — treat as unknown and let the caller use the generic message.
            return null;
        }
    }
}
