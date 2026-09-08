using AtelieBebe.Orders.Core.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Orders.Core.Infrastructure;

public sealed class AppUrlProvider : IAppUrlProvider
{
    public string PublicUrl { get; }

    public AppUrlProvider(IOptions<AppUrlOptions> options) => PublicUrl = options.Value.PublicUrl;
}
