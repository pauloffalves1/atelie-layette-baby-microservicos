using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.SharedKernel.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(string currency = "BRL") => new(0m, currency);

    public static Money FromReais(decimal amount, string currency = "BRL")
    {
        if (amount < 0) throw new DomainException("O valor monetário não pode ser negativo.");
        return new Money(Math.Round(amount, 2, MidpointRounding.AwayFromZero), currency);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>Clamped at zero — a discount larger than the amount being discounted never produces a negative total.</summary>
    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Math.Max(0m, Math.Round(Amount - other.Amount, 2, MidpointRounding.AwayFromZero)), Currency);
    }

    public Money Multiply(int factor)
    {
        if (factor < 0) throw new DomainException("O fator de multiplicação não pode ser negativo.");
        return new Money(Math.Round(Amount * factor, 2, MidpointRounding.AwayFromZero), Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Não é possível operar valores em moedas diferentes.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Currency} {Amount:0.00}";
}
