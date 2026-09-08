using System.Text.RegularExpressions;

namespace AtelieBebe.Notifications.Worker.Infrastructure;

/// <summary>
/// Best-effort normalization of a Brazilian phone number into the E.164-ish digit string
/// (country code + digits, no "+", no punctuation) the WhatsApp Cloud API expects as "to".
/// Doesn't validate DDD or the mobile 9th digit — just strips formatting and assumes Brazil
/// when no country code is present.
/// </summary>
public static class WhatsAppPhoneFormatter
{
    public static string ToE164(string phone)
    {
        var digits = Regex.Replace(phone, @"\D", "");

        if (!digits.StartsWith("55") && digits.Length is 10 or 11)
            digits = "55" + digits;

        return digits;
    }
}
