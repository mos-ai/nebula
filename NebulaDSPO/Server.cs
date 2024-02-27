using System.Collections.Generic;
using System.Linq;
using System.Net;
using EasyR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NebulaAPI.DataStructures;
using NebulaAPI.GameState;
using NebulaAPI.Networking;
using NebulaDSPO.ServerCore.Services;
using NebulaModel;
using NebulaModel.Networking;
using NebulaModel.Packets.GameHistory;
using NebulaWorld;
using UnityEngine;

namespace NebulaDSPO;

public class Server : IServer
{
    private bool _disposedValue;

    private const float GAME_RESEARCH_UPDATE_INTERVAL = 2;
    private const float STATISTICS_UPDATE_INTERVAL = 1;
    private const float LAUNCH_UPDATE_INTERVAL = 4;
    private const float DYSONSPHERE_UPDATE_INTERVAL = 2;
    private const float WARNING_UPDATE_INTERVAL = 1;

    private readonly bool loadSaveFile;

    private float dysonLaunchUpateTimer = 1;
    private float dysonSphereUpdateTimer;

    private float gameResearchHashUpdateTimer;
    private float productionStatisticsUpdateTimer;

    private float warningUpdateTimer;

    private IHost? host;
    private ILogger<Server>? logger;

    private ServerCore.Hubs.Internal.GenericHub? genericHub;
    private ServerCore.Hubs.Internal.GenericHubProxy? genericHubProxy;
    private ServerManager? serverManager;

    public IPEndPoint ServerEndpoint { get; set; }
    public ushort Port { get; set; }
    public string NgrokAddress => string.Empty;
    public bool NgrokActive => false;
    public bool NgrokEnabled => false;
    public string NgrokLastErrorCode => string.Empty;
    public ConcurrentPlayerCollection Players { get; } = new();
    public INetPacketProcessor PacketProcessor => this.genericHub?.PacketProcessor ?? throw new ApplicationException("PacketProcessor not initialised. Have you started a game?");

    public event EventHandler<INebulaConnection>? Connected;
    public event EventHandler<INebulaConnection>? Disconnected;

    public Server(string url, int port, string password, bool loadSaveFile = false)
        : this(new IPEndPoint(Dns.GetHostEntry(url).AddressList[0], port), password, loadSaveFile)
    {
    }

    public Server(IPEndPoint endpoint, string password, bool loadSaveFile = false)
    {
        ServerEndpoint = endpoint;
        this.loadSaveFile = loadSaveFile;
    }

    public Server(ushort port, bool loadSaveFile = false)
    {
        ServerEndpoint = new IPEndPoint(IPAddress.Loopback, port);
        this.loadSaveFile = loadSaveFile;
    }

    public void Disconnect(INebulaConnection conn, DisconnectionReason reason, string reasonMessage = "")
    {
        this.serverManager?.OnPlayerDisconnected(new ServerCore.Models.Internal.NebulaConnection(conn.Id));
    }

    public void SendPacket<T>(T packet) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketAsync(packet).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /serverCore/genericHub/send"));
    }

    public void SendPacketExclude<T>(T packet, INebulaConnection exclude) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketExcludeAsync(packet, new ServerCore.Models.Internal.NebulaConnection(exclude.Id))
            .SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /serverCore/genericHub/sendExclude"));
    }

    public void SendPacketToLocalPlanet<T>(T packet) where T : class, new()
    {
        var planetId = GameMain.data.localPlanet?.id ?? -1;

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        SendPacketToPlanet(packet, planetId);
    }

    public void SendPacketToLocalStar<T>(T packet) where T : class, new()
    {
        var starId = GameMain.data.localStar?.id ?? -1;

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        SendPacketToStar(packet, starId);
    }

    public void SendPacketToPlanet<T>(T packet, int planetId) where T : class, new()
    {
        var players = Players.Connected
            .Where(kvp => kvp.Value.Data.LocalPlanetId == planetId);

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        SendToPlayers(players, packet);
    }

    public Task SendPacketToPlanetAsync(byte[] rawData, int planetId)
    {
        var players = Players.Connected
            .Where(kvp => kvp.Value.Data.LocalPlanetId == planetId);

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        return SendToPlayersAsync(players, rawData);
    }

    public void SendPacketToStar<T>(T packet, int starId) where T : class, new()
    {
        var players = Players.Connected
            .Where(kvp => kvp.Value.Data.LocalStarId == starId);

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        SendToPlayers(players, packet);
    }

    public void SendPacketToStarExclude<T>(T packet, int starId, INebulaConnection exclude) where T : class, new()
    {
        var players = Players.Connected
            .Where(kvp => kvp.Value.Data.LocalStarId == starId && kvp.Key.Id != exclude.Id);

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        SendToPlayers(players, packet);
    }

    public void SendToMatching<T>(T packet, Predicate<INebulaPlayer> condition) where T : class, new()
    {
        var players = Players.Connected
            .Where(kvp => condition(kvp.Value));

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        SendToPlayers(players, packet);
    }

    public void SendToPlayers<T>(IEnumerable<KeyValuePair<INebulaConnection, INebulaPlayer>> players, T packet) where T : class, new()
    {
        var playerConnections = players.Select(player => new ServerCore.Models.Internal.NebulaConnection(player.Key.Id));
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendToPlayersAsync(playerConnections, packet)
            .SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /serverCore/genericHub/sendToPlayers"));
    }

    public Task SendToPlayersAsync(IEnumerable<KeyValuePair<INebulaConnection, INebulaPlayer>> players, byte[] rawData)
    {
        var playerConnections = players.Select(player => new ServerCore.Models.Internal.NebulaConnection(player.Key.Id));
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        try
        {
            return genericHubProxy.SendToPlayersAsync(playerConnections, rawData);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to call /serverCore/genericHub/sendToPlayers");
        }

        return Task.CompletedTask;
    }

    public void Start()
    {
        if (this.host is not null)
            Stop();

        var builder = new HostBuilder();
        builder.ConfigureLogging(logBuilder =>
        {
            logBuilder.AddConsole();
            logBuilder.SetMinimumLevel(LogLevel.Trace);
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(serviceProvider => CreateSocketConnection(ServerEndpoint));
            services.AddSingleton<ConnectionService>();
            services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ConnectionService>());

            // Hubs
            //services.AddSingleton<ServerCore.Hubs.Chat>();
            //services.AddSingleton<ServerCore.Hubs.Factory>();
            //services.AddSingleton<ServerCore.Hubs.GameHistory>();
            //services.AddSingleton<ServerCore.Hubs.Logistics>();
            //services.AddSingleton<ServerCore.Hubs.Planet>();
            //services.AddSingleton<ServerCore.Hubs.Players>();
            //services.AddSingleton<ServerCore.Hubs.Routers>();
            //services.AddSingleton<ServerCore.Hubs.Session>();
            //services.AddSingleton<ServerCore.Hubs.Statistics>();
            //services.AddSingleton<ServerCore.Hubs.Trash>();
            //services.AddSingleton<ServerCore.Hubs.Universe>();
            //services.AddSingleton<ServerCore.Hubs.Warning>();
            // Catch all Hub
            services.AddSingleton<ServerCore.Hubs.Internal.GenericHub>();
            services.AddSingleton<ServerCore.Hubs.Internal.PlayerConnectionHub>();

            // Client Proxies
            //services.AddSingleton<ServerCore.Hubs.ChatProxy>();
            //services.AddSingleton<ServerCore.Hubs.FactoryProxy>();
            //services.AddSingleton<ServerCore.Hubs.GameHistoryProxy>();
            //services.AddSingleton<ServerCore.Hubs.LogisticsProxy>();
            //services.AddSingleton<ServerCore.Hubs.PlanetProxy>();
            //services.AddSingleton<ServerCore.Hubs.PlayersProxy>();
            //services.AddSingleton<ServerCore.Hubs.RoutersProxy>();
            //services.AddSingleton<ServerCore.Hubs.SessionProxy>();
            //services.AddSingleton<ServerCore.Hubs.StatisticsProxy>();
            //services.AddSingleton<ServerCore.Hubs.TrashProxy>();
            //services.AddSingleton<ServerCore.Hubs.UniverseProxy>();
            //services.AddSingleton<ServerCore.Hubs.WarningProxy>();
            // Catch all Client Proxy
            services.AddSingleton<ServerCore.Hubs.Internal.GenericHubProxy>();
            services.AddSingleton<ServerCore.Hubs.Internal.PlayerConnectionHubProxy>();

            // Server Resources
            services.AddSingleton(serviceProvider => ActivatorUtilities.CreateInstance<ServerManager>(serviceProvider, this.loadSaveFile));
        });

        this.host = builder.Build();

        // Start Client
        this.host.Start();

        this.genericHub = host.Services.GetRequiredService<ServerCore.Hubs.Internal.GenericHub>();
        this.genericHubProxy = host.Services.GetRequiredService<ServerCore.Hubs.Internal.GenericHubProxy>();
        this.serverManager = host.Services.GetRequiredService<ServerManager>();
    }

    public void Stop()
    {
        this.host?.StopAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false).GetAwaiter().GetResult();
        this.host = null;
    }

    public void Update()
    {
        this.genericHub?.PacketProcessor.ProcessPacketQueue();

        if (!Multiplayer.Session.IsGameLoaded)
        {
            return;
        }

        gameResearchHashUpdateTimer += Time.deltaTime;
        productionStatisticsUpdateTimer += Time.deltaTime;
        dysonLaunchUpateTimer += Time.deltaTime;
        dysonSphereUpdateTimer += Time.deltaTime;
        warningUpdateTimer += Time.deltaTime;

        if (gameResearchHashUpdateTimer > GAME_RESEARCH_UPDATE_INTERVAL)
        {
            gameResearchHashUpdateTimer = 0;
            if (GameMain.data.history.currentTech != 0)
            {
                var state = GameMain.data.history.techStates[GameMain.data.history.currentTech];
                SendPacket(new GameHistoryResearchUpdatePacket(GameMain.data.history.currentTech, state.hashUploaded,
                    state.hashNeeded, GameMain.statistics.techHashedFor10Frames));
            }
        }

        if (productionStatisticsUpdateTimer > STATISTICS_UPDATE_INTERVAL)
        {
            productionStatisticsUpdateTimer = 0;
            Multiplayer.Session.Statistics.SendBroadcastIfNeeded();
        }

        if (dysonLaunchUpateTimer > LAUNCH_UPDATE_INTERVAL)
        {
            dysonLaunchUpateTimer = 0;
            Multiplayer.Session.Launch.SendBroadcastIfNeeded();
        }

        if (dysonSphereUpdateTimer > DYSONSPHERE_UPDATE_INTERVAL)
        {
            dysonSphereUpdateTimer = 0;
            Multiplayer.Session.DysonSpheres.UpdateSphereStatusIfNeeded();
        }

        if (!(warningUpdateTimer > WARNING_UPDATE_INTERVAL))
        {
            return;
        }

        warningUpdateTimer = 0;
        Multiplayer.Session.Warning.SendBroadcastIfNeeded();
    }

    private static HubConnection CreateSocketConnection(IPEndPoint? endpoint = null)
    {
        endpoint ??= new IPEndPoint(IPAddress.Loopback, 9000);
        if (endpoint.Port == 0)
            endpoint.Port = 9000;

        ((LocalPlayer)Multiplayer.Session.LocalPlayer).IsHost = true;

        if (Config.Options.RememberLastIP)
        {
            // We've successfully connected, set connection as last ip, cutting out "ws://" and "/socket"
            Config.Options.LastIP = endpoint.ToString();
            Config.SaveOptions();
        }

        var builder = new HubConnectionBuilder();
        builder.AddNewtonsoftJsonProtocol(options =>
        {
            options.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
        });
        //builder.AddStructPackProtocol();
        builder.WithSocket(endpoint);
        return builder.Build();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Stop();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Client()
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
