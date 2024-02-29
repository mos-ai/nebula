using System.Threading;
using EasyR.Client;
using NebulaDSPO.Hubs.Internal;
using NebulaDSPO.ServerCore.Models.Internal;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs.Internal;

/// <summary>
/// <para>A Player Hub to handle when users connect and disconnect to the server.</para>
/// <para>In the original client the host was actually the server so it was triggered by socket events. In this implementation we have to get these events passed to us by the server.</para>
/// </summary>
internal class PlayerConnectionHub
{
    private readonly ServerManager serverManager;

    public PlayerConnectionHub(ConnectionService connection, ServerCore.Services.ServerManager serverManager)
    {
        this.serverManager = serverManager;

        connection.RegisterEndpoint(ep => ep.On<string>("/serverCore/playerConnectionHub/connected", this.serverManager.OnPlayerConnected));
        connection.RegisterEndpoint(ep => ep.On<NebulaConnection>("/serverCore/playerConnectionHub/disconnected", this.serverManager.OnPlayerDisconnected));
    }
}

internal class PlayerConnectionHubProxy
{
    private readonly HubConnection connection;

    public PlayerConnectionHubProxy(HubConnection connection)
    {
        this.connection = connection;
    }

    public Task ServerConnectedAsync(CancellationToken cancellationToken = default)
        => this.connection.InvokeAsync("/serverCore/playerConnectionHub/serverConnected", cancellationToken);

    public Task PlayerConnectedAsync(string connectionId, int playerId, CancellationToken cancellationToken = default)
        => this.connection.InvokeAsync("/serverCore/playerConnectionHub/playerConnected", connectionId, playerId, cancellationToken);
}
