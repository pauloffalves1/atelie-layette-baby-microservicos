using System.Text.RegularExpressions;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.SharedKernel.ValueObjects;

public sealed partial class Cpf : ValueObject
{
    public string Value { get; }

    private Cpf(string value) => Value = value;

    public static Cpf Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("O CPF é obrigatório.");

        var digits = NonDigitRegex().Replace(value, "");

        if (digits.Length != 11 || AllDigitsEqualRegex().IsMatch(digits) || !HasValidCheckDigits(digits))
            throw new DomainException($"O CPF '{value}' não é válido.");

        return new Cpf(digits);
    }

    private static bool HasValidCheckDigits(string digits)
    {
        var numbers = digits.Select(c => c - '0').ToArray();

        var firstCheck = ComputeCheckDigit(numbers, 9, 10);
        if (firstCheck != numbers[9]) return false;

        var secondCheck = ComputeCheckDigit(numbers, 10, 11);
        return secondCheck == numbers[10];
    }

    private static int ComputeCheckDigit(int[] numbers, int digitCount, int firstWeight)
    {
        var sum = 0;
        for (var i = 0; i < digitCount; i++)
            sum += numbers[i] * (firstWeight - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();

    [GeneratedRegex(@"^(\d)\1{10}$")]
    private static partial Regex AllDigitsEqualRegex();
}
