using EasyR.Client;
using Microsoft.Extensions.Logging;
using NebulaDSPO.ServerCore.Services;
using NebulaWorld;

namespace NebulaDSPO.ServerCore.Hubs.Internal;

/// <summary>
/// <para>A Player Hub to handle when users connect and disconnect to the server.</para>
/// <para>In the original client the host was actually the server so it was triggered by socket events. In this implementation we have to get these events passed to us by the server.</para>
/// </summary>
internal class PlayerConnectionHub
{
    private readonly ServerManager serverManager;
    private readonly ILogger<PlayerConnectionHub> logger;

    public PlayerConnectionHub(ConnectionService connection, ServerCore.Services.ServerManager serverManager, ILogger<PlayerConnectionHub> logger)
    {
        logger.LogInformation("Initialised {HubName}.", nameof(PlayerConnectionHub));

        this.serverManager = serverManager;
        this.logger = logger;

        connection.RegisterEndpoint(ep => ep.On<string>("/serverCore/playerConnectionHub/connected", OnPlayerConnected));
        connection.RegisterEndpoint(ep => ep.On<string>("/serverCore/playerConnectionHub/disconnected", OnPlayerDisconnected));
    }

    internal void OnPlayerConnected(string connectionId)
    {
        this.logger.LogInformation("Player connected: {ConnectionId}", connectionId);
        this.serverManager.OnPlayerConnected(connectionId);
    }

    internal void OnPlayerDisconnected(string connectionId)
    {
        this.logger.LogInformation("Player disconnected: {ConnectionId}", connectionId);
        if (!((Server)Multiplayer.Session.Server).PlayerConnections.TryGetValue(connectionId, out var connection))
        {
            this.logger.LogInformation("Player disconnected (No match Found): {ConnectionId}", connectionId);
            return;
        }

        this.logger.LogInformation("Player disconnected: {ConnectionId}, Id: {PlayerId}", connectionId, connection.Id);

        this.serverManager.OnPlayerDisconnected(connectionId, connection);
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
        => this.connection.SendAsync("/serverCore/playerConnectionHub/serverConnected", cancellationToken);

    public Task PlayerConnectedAsync(string connectionId, int playerId, CancellationToken cancellationToken = default)
        => this.connection.SendAsync("/serverCore/playerConnectionHub/playerConnected", connectionId, playerId, cancellationToken);
}
