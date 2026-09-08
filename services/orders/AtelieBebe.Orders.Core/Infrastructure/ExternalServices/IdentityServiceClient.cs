using System.Net;
using System.Net.Http.Json;
using AtelieBebe.Orders.Core.Application.Abstractions;

namespace AtelieBebe.Orders.Core.Infrastructure.ExternalServices;

public sealed class IdentityServiceClient : IIdentityServiceClient
{
    private readonly HttpClient _httpClient;

    public IdentityServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IdentityCustomerInfo?> GetCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/internal/customers/{customerId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IdentityCustomerInfo>(cancellationToken: ct);
    }
}
