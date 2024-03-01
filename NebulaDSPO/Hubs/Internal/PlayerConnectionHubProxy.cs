using EasyR.Client;

namespace NebulaDSPO.Hubs.Internal;
internal class PlayerConnectionHubProxy
{
    private readonly HubConnection connection;

    public PlayerConnectionHubProxy(HubConnection connection)
    {
        this.connection = connection;
    }

    public Task PlayerConnectedAsync(CancellationToken cancellationToken = default)
        => this.connection.SendAsync("/playerConnectionHub/connected", cancellationToken);
}
