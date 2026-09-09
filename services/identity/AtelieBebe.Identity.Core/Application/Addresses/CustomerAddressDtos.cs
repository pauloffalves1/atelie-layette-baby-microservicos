namespace AtelieBebe.Identity.Core.Application.Addresses;

public sealed record CustomerAddressDto(
    Guid Id, string Label, string Street, string Number, string? Complement,
    string Neighborhood, string City, string State, string ZipCode, bool IsDefault);

public sealed record SaveCustomerAddressRequest(
    string Label, string Street, string Number, string? Complement,
    string Neighborhood, string City, string State, string ZipCode, bool IsDefault);
