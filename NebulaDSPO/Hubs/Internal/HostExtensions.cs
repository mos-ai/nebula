using EasyR.Client;
using Microsoft.Extensions.Hosting;
using NebulaDSPO.Hubs.Internal;

namespace Microsoft.Extensions.DependencyInjection;

internal static class HostExtensions
{
    internal static IHost MapEndpoint<T>(this IHost host) where T : class, IHubListener
    {
        var connection = host.Services.GetRequiredService<HubConnection>();
        var endpointService = host.Services.GetRequiredService<T>();
        endpointService.RegisterEndPoints(connection);

        return host;
    }
}
