using EasyR.Client;
using NebulaDSPO.Hubs.Internal;
using NebulaDSPO.ServerCore.Models.Internal;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs.Internal;

/// <summary>
/// <para>A Player Hub to handle when users connect and disconnect to the server.</para>
/// <para>In the original client the host was actually the server so it was triggered by socket events. In this implementation we have to get these events passed to us by the server.</para>
/// </summary>
internal class PlayerConnectionHub : HubListener
{
    private readonly ServerManager serverManager;

    public PlayerConnectionHub(ServerCore.Services.ServerManager serverManager)
    {
        this.serverManager = serverManager;
    }

    public override void RegisterEndPoints(HubConnection connection)
    {
        RegisterEndPoint(connection.On("/playerConnectionHub/playerCconnected", this.serverManager.OnPlayerConnected));
        RegisterEndPoint(connection.On<NebulaConnection>("/playerConnectionHub/playerDisconnected", this.serverManager.OnPlayerDisconnected));
    }
}

internal class PlayerConnectionHubProxy
{
    private readonly HubConnection connection;

    public PlayerConnectionHubProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
