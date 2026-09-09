using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Identity.Core.Domain.Entities;

/// <summary>One of a customer's saved delivery addresses — picked from a list at checkout instead of retyped every time. Independent of Customer's own AddressStreet/etc fields, which stay as the account's single "default" address for backward compatibility.</summary>
public sealed class CustomerAddress : Entity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public string Label { get; private set; } = default!;
    public string Street { get; private set; } = default!;
    public string Number { get; private set; } = default!;
    public string? Complement { get; private set; }
    public string Neighborhood { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string State { get; private set; } = default!;
    public string ZipCode { get; private set; } = default!;
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CustomerAddress() { } // EF Core

    private CustomerAddress(Guid id, Guid customerId, string label, string street, string number, string? complement,
        string neighborhood, string city, string state, string zipCode, bool isDefault) : base(id)
    {
        CustomerId = customerId;
        Label = label;
        Street = street;
        Number = number;
        Complement = complement;
        Neighborhood = neighborhood;
        City = city;
        State = state;
        ZipCode = zipCode;
        IsDefault = isDefault;
        CreatedAt = DateTime.UtcNow;
    }

    public static CustomerAddress Create(Guid customerId, string label, string street, string number, string? complement,
        string neighborhood, string city, string state, string zipCode, bool isDefault = false)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Cliente inválido.");
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("Dê um nome para este endereço (ex: Casa, Trabalho).");
        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(neighborhood) ||
            string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(zipCode))
            throw new DomainException("Preencha todos os campos obrigatórios do endereço.");

        return new CustomerAddress(Guid.NewGuid(), customerId, label.Trim(), street.Trim(), number.Trim(),
            string.IsNullOrWhiteSpace(complement) ? null : complement.Trim(), neighborhood.Trim(), city.Trim(),
            state.Trim().ToUpperInvariant(), zipCode.Trim(), isDefault);
    }

    public void Update(string label, string street, string number, string? complement, string neighborhood, string city, string state, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("Dê um nome para este endereço (ex: Casa, Trabalho).");
        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(neighborhood) ||
            string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(zipCode))
            throw new DomainException("Preencha todos os campos obrigatórios do endereço.");

        Label = label.Trim();
        Street = street.Trim();
        Number = number.Trim();
        Complement = string.IsNullOrWhiteSpace(complement) ? null : complement.Trim();
        Neighborhood = neighborhood.Trim();
        City = city.Trim();
        State = state.Trim().ToUpperInvariant();
        ZipCode = zipCode.Trim();
    }

    public void MarkAsDefault() => IsDefault = true;
    public void UnmarkAsDefault() => IsDefault = false;
}
