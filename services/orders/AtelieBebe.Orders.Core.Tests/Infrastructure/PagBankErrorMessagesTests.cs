using AtelieBebe.Orders.Core.Infrastructure.Payments;

namespace AtelieBebe.Orders.Core.Tests.Infrastructure;

/// <summary>
/// Which PagBank refusals deserve their own message instead of "tente novamente em instantes" — a
/// distinction that matters because retrying a permanent refusal never works.
/// </summary>
public class PagBankErrorMessagesTests
{
    /// <summary>The exact body production returned when the buyer's e-mail was the store's own.</summary>
    private const string BuyerIsMerchantBody =
        """{"error_messages":[{"code":"40002","description":"buyer email must not be equals to merchant email","parameter_name":"customer.email"}]}""";

    [Fact]
    public void BuyerEmailEqualsMerchantEmail_ExplainsWhatToDo()
    {
        var message = PagBankErrorMessages.ForCustomer(BuyerIsMerchantBody);

        Assert.NotNull(message);
        Assert.Contains("e-mail", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("instantes", message);
    }

    [Fact]
    public void UnknownErrorCode_FallsBackToTheGenericMessage()
    {
        var body = """{"error_messages":[{"code":"40001","description":"required parameter","parameter_name":"customer.tax_id"}]}""";

        Assert.Null(PagBankErrorMessages.ForCustomer(body));
    }

    [Fact]
    public void SameCodeOnAnotherField_IsNotTreatedAsTheEmailRefusal()
    {
        var body = """{"error_messages":[{"code":"40002","description":"invalid value","parameter_name":"customer.name"}]}""";

        Assert.Null(PagBankErrorMessages.ForCustomer(body));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<html>502 Bad Gateway</html>")]
    [InlineData("{\"unexpected\":true}")]
    public void UnusableBody_FallsBackToTheGenericMessage(string? body)
    {
        Assert.Null(PagBankErrorMessages.ForCustomer(body));
    }
}
