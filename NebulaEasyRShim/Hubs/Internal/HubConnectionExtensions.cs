using EasyR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace NebulaEasyRShim.Hubs.Internal;

internal static class HubConnectionExtensions
{
    private static IServiceProvider Services;

    internal static HubConnection MapEndpoint<T>(this HubConnection connection) where T : class, IHubListener
    {
        Services ??= Cloud.Host.Services;

        var endpointService = Services.GetRequiredService<T>();
        endpointService.RegisterEndPoints(connection);

        return connection;
    }
}
