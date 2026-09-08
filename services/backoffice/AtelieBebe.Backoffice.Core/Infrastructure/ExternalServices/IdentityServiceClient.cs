using System.Net.Http.Json;
using AtelieBebe.Backoffice.Core.Application.Abstractions;

namespace AtelieBebe.Backoffice.Core.Infrastructure.ExternalServices;

public sealed class IdentityServiceClient : IIdentityServiceClient
{
    private readonly HttpClient _httpClient;

    public IdentityServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<int> GetCustomerCountAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<CountResponse>("/internal/customers/count", ct);
        return result?.Count ?? 0;
    }

    private sealed record CountResponse(int Count);
}
