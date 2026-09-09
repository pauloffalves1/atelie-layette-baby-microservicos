namespace AtelieBebe.Identity.Core.Application.Customers;

public sealed record CustomerSummaryDto(
    Guid Id, string Name, string Email, string? Phone, string? Cpf, DateTime CreatedAt, bool IsAnonymized, bool EmailVerified,
    string? AddressStreet, string? AddressNumber, string? AddressComplement,
    string? AddressNeighborhood, string? AddressCity, string? AddressState, string? AddressZipCode);

public sealed record UpdateCustomerRequest(
    string Name, string Email, string Cpf, string? Phone,
    string? AddressStreet = null, string? AddressNumber = null, string? AddressComplement = null,
    string? AddressNeighborhood = null, string? AddressCity = null, string? AddressState = null, string? AddressZipCode = null);
