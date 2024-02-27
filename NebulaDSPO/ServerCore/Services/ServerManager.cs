using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NebulaAPI;
using NebulaAPI.DataStructures;
using NebulaAPI.GameState;
using NebulaAPI.Networking;
using NebulaDSPO.ServerCore.Hubs.Internal;
using NebulaDSPO.ServerCore.Models.Internal;
using NebulaModel;
using NebulaModel.DataStructures;
using NebulaModel.Packets.Players;
using NebulaModel.Packets.Session;
using NebulaWorld;
using NebulaWorld.SocialIntegration;

namespace NebulaDSPO.ServerCore.Services;

internal class ServerManager : IDisposable
{
    private bool _disposedValue;

    private readonly ConnectionService connection;
    private readonly PlayerConnectionHubProxy playerConnectionHubProxy;
    private readonly ILogger<ServerManager> logger;

    private readonly bool loadSaveFile;

    private ConcurrentQueue<ushort> PlayerIdPool = new();
    private int highestPlayerID;

    private List<IDisposable> events = new();

    public ConcurrentPlayerCollection Players { get; } = new();

    public event EventHandler<INebulaConnection>? Connected;
    public event EventHandler<INebulaConnection>? Disconnected;

    public ServerManager(ConnectionService connection, PlayerConnectionHubProxy playerConnectionHubProxy, bool loadSaveFile, ILogger<ServerManager> logger)
    {
        this.connection = connection;
        this.playerConnectionHubProxy = playerConnectionHubProxy;
        this.logger = logger;

        this.loadSaveFile = loadSaveFile;

        this.events.Add(this.connection.ConectionChanged.Subscribe(ConnectionChanged));

        // Check if already connected, if so initialise the game.
        if (connection.IsConnected)
            OnConnected();
    }

    internal void OnPlayerConnected(string connectionId)
    {
        // Generate new data for the player
        var playerId = GetNextPlayerId();

        // this is truncated to ushort.MaxValue
        var birthPlanet = GameMain.galaxy.PlanetById(GameMain.galaxy.birthPlanetId);
        var playerData = new PlayerData(playerId, -1,
            position: new Double3(birthPlanet.uPosition.x, birthPlanet.uPosition.y, birthPlanet.uPosition.z));

        var playerConnection = new NullNebulaConnection(playerId);

        playerConnection.ConnectionStatus = EConnectionStatus.Pending;

        INebulaPlayer newPlayer = new NebulaPlayer(playerConnection, playerData);
        if (!Players.TryAdd(playerConnection, newPlayer))
            throw new InvalidOperationException($"Connection {playerConnection.Id} already exists!");

        this.playerConnectionHubProxy.PlayerConnectedAsync(connectionId, newPlayer.Id).SafeFireAndForget(ex => this.logger.LogError(ex, "Failed to call /serverCore/playerConnectionHub/playerConnected"));

        // return newPlayer;
        Connected?.Invoke(this, playerConnection);
    }

    internal void OnPlayerDisconnected(NebulaConnection connection)
    {
        var playerConnection = Players.Connected.Keys.Any(player => player.Id == connection.Id) ?
            Players.Connected.First(player => player.Key.Id == connection.Id).Key
            : new NullNebulaConnection(connection);

        Multiplayer.Session.NumPlayers -= 1;
        DiscordManager.UpdateRichPresence();

        Players.TryRemove(playerConnection, out var player);

        // @TODO: Why can this happen in the first place?
        // Figure out why it was possible before the move and fix that issue at the root.
        if (player is null)
        {
            this.logger.LogWarning("Player is null - Disconnect logic NOT CALLED!");

            if (!Config.Options.SyncSoil)
            {
                return;
            }

            // now we need to recalculate the current sand amount :C
            GameMain.mainPlayer.sandCount = Multiplayer.Session.LocalPlayer.Data.Mecha.SandCount;
            // using (GetConnectedPlayers(out var connectedPlayers))
            {
                var connectedPlayers = Players.Connected;
                foreach (var entry in connectedPlayers)
                {
                    GameMain.mainPlayer.sandCount += entry.Value.Data.Mecha.SandCount;
                }
            }

            UIRoot.instance.uiGame.OnSandCountChanged(GameMain.mainPlayer.sandCount,
                GameMain.mainPlayer.sandCount - Multiplayer.Session.LocalPlayer.Data.Mecha.SandCount);
            Multiplayer.Session.Server.SendPacket(new PlayerSandCount(GameMain.mainPlayer.sandCount));

            return;
        }

        // player is valid
        Multiplayer.Session.Server.SendPacketExclude(new PlayerDisconnected(player.Id, Multiplayer.Session.NumPlayers), playerConnection);
        // For sync completed player who triggered OnPlayerJoinedGame() before
        if (playerConnection.ConnectionStatus == EConnectionStatus.Connected)
        {
            SimulatedWorld.OnPlayerLeftGame(player);
        }

        PlayerIdPool.Enqueue(player.Id);

        Multiplayer.Session.PowerTowers.ResetAndBroadcast();
        Multiplayer.Session.Statistics.UnRegisterPlayer(player.Id);
        Multiplayer.Session.DysonSpheres.UnRegisterPlayer(playerConnection);

        // Note: using Keys or Values directly creates a readonly snapshot at the moment of call, as opposed to enumerating the dict.
        var syncCount = Players.Syncing.Count;
        if (playerConnection.ConnectionStatus is not EConnectionStatus.Syncing || syncCount != 0)
        {
            return;
        }

        Multiplayer.Session.Server.SendPacket(new SyncComplete());
        Multiplayer.Session.World.OnAllPlayersSyncCompleted();
        Disconnected?.Invoke(this, playerConnection);
    }

    private void ConnectionChanged(bool newState)
    {
        if (newState)
        {
            OnConnected();
        }
        else
        {
            OnDisconnected();
        }
    }

    private void OnConnected()
    {
        this.logger.LogTrace("Server Connected");

        if (this.loadSaveFile)
        {
            this.logger.LogTrace("Loading Save File");
            SaveManager.LoadServerData();
        }

        this.logger.LogTrace("Populating LocalPlayer");
        ((LocalPlayer)Multiplayer.Session.LocalPlayer).SetPlayerData(new PlayerData(
                GetNextPlayerId(),
                GameMain.localPlanet?.id ?? -1,
                !string.IsNullOrWhiteSpace(Config.Options.Nickname) ? Config.Options.Nickname : GameMain.data.account.userName),
            this.loadSaveFile);

        this.playerConnectionHubProxy.ServerConnectedAsync()
            .ContinueWith(result =>
            {
                try
                {
                    this.logger.LogTrace("Notifying Game Ready");
                    NebulaModAPI.OnMultiplayerSessionChange(true);
                    NebulaModAPI.OnMultiplayerGameStarted?.Invoke();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "OnConnected error: {Message}", ex.Message);
                }
                return Task.CompletedTask;
            })
            .SafeFireAndForget(ex =>
            {
                this.logger.LogError(ex, "Failed to register Server with Gateway");
                // TODO: Shutdown session.
            });
    }

    private void OnDisconnected()
    {
        try
        {
            NebulaModAPI.OnMultiplayerSessionChange(false);
            NebulaModAPI.OnMultiplayerGameEnded?.Invoke();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "OnDisconnected error: {Message}", ex.Message);
        }
    }

    private ushort GetNextPlayerId()
    {
        if (!PlayerIdPool.TryDequeue(out var nextId))
            nextId = (ushort)Interlocked.Increment(ref highestPlayerID);
        return nextId;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                foreach (var @event in this.events)
                {
                    @event.Dispose();
                }

                this.events.Clear();
                this.events = null!;
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ServerManager()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
