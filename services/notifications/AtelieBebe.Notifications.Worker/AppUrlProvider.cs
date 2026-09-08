using AtelieBebe.Notifications.Worker.Abstractions;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Notifications.Worker;

public sealed class AppUrlProvider : IAppUrlProvider
{
    public string PublicUrl { get; }

    public AppUrlProvider(IOptions<AppUrlOptions> options) => PublicUrl = options.Value.PublicUrl;
}
