using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EasyR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NebulaAPI.DataStructures;
using NebulaAPI.GameState;
using NebulaAPI.Networking;
using NebulaDSPO.ServerCore.Services;
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

    public Server(string url, int port, bool loadSaveFile = false)
        : this(new IPEndPoint(Dns.GetHostEntry(url).AddressList[0], port), loadSaveFile)
    {
    }

    public Server(IPEndPoint endpoint, bool loadSaveFile = false)
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
        this.serverManager.OnPlayerDisconnected(new ServerCore.Models.Internal.NebulaConnection(conn.Id));
    }

    public void SendPacket<T>(T packet) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketAsync(packet).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/send"));
    }

    public void SendPacketExclude<T>(T packet, INebulaConnection exclude) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketExcludeAsync(packet, exclude).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendExclude"));
    }

    public void SendPacketToLocalPlanet<T>(T packet) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketToLocalPlanetAsync(packet).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToLocalPlanet"));
    }

    public void SendPacketToLocalStar<T>(T packet) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketToLocalStarAsync(packet).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToLocalStar"));
    }

    public void SendPacketToPlanet<T>(T packet, int planetId) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketToPlanetAsync(packet, planetId).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToPlanet"));
    }

    public void SendPacketToStar<T>(T packet, int starId) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketToStarAsync(packet, starId).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToStar"));
    }

    public void SendPacketToStarExclude<T>(T packet, int starId, INebulaConnection exclude) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketToStarExcludeAsync(packet, starId, exclude).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToStarExclusive"));
    }

    public void SendToMatching<T>(T packet, Predicate<INebulaPlayer> condition) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendToMatchingAsync(packet, condition).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToMatching"));
    }

    public void SendToPlayers<T>(IEnumerable<KeyValuePair<INebulaConnection, INebulaPlayer>> players, T packet) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendToPlayersAsync(players, packet).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToPlayers"));
    }

    public void Start()
    {
        if (this.host is not null) Stop();

        var builder = new HostBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(serviceProvider => CreateSocketConnection(ServerEndpoint));
            services.AddHostedService<ServerCore.Services.ConnectionService>();

            // Hubs
            services.AddSingleton<ServerCore.Hubs.Chat>();
            services.AddSingleton<ServerCore.Hubs.Factory>();
            services.AddSingleton<ServerCore.Hubs.GameHistory>();
            services.AddSingleton<ServerCore.Hubs.Logistics>();
            services.AddSingleton<ServerCore.Hubs.Planet>();
            services.AddSingleton<ServerCore.Hubs.Players>();
            services.AddSingleton<ServerCore.Hubs.Routers>();
            services.AddSingleton<ServerCore.Hubs.Session>();
            services.AddSingleton<ServerCore.Hubs.Statistics>();
            services.AddSingleton<ServerCore.Hubs.Trash>();
            services.AddSingleton<ServerCore.Hubs.Universe>();
            services.AddSingleton<ServerCore.Hubs.Warning>();
            // Catch all Hub
            services.AddSingleton<ServerCore.Hubs.Internal.GenericHub>();

            // Client Proxies
            services.AddSingleton<ServerCore.Hubs.ChatProxy>();
            services.AddSingleton<ServerCore.Hubs.FactoryProxy>();
            services.AddSingleton<ServerCore.Hubs.GameHistoryProxy>();
            services.AddSingleton<ServerCore.Hubs.LogisticsProxy>();
            services.AddSingleton<ServerCore.Hubs.PlanetProxy>();
            services.AddSingleton<ServerCore.Hubs.PlayersProxy>();
            services.AddSingleton<ServerCore.Hubs.RoutersProxy>();
            services.AddSingleton<ServerCore.Hubs.SessionProxy>();
            services.AddSingleton<ServerCore.Hubs.StatisticsProxy>();
            services.AddSingleton<ServerCore.Hubs.TrashProxy>();
            services.AddSingleton<ServerCore.Hubs.UniverseProxy>();
            services.AddSingleton<ServerCore.Hubs.WarningProxy>();
            // Catch all Client Proxy
            services.AddSingleton<ServerCore.Hubs.Internal.GenericHubProxy>();

            // Server Resources
            services.AddSingleton(serviceProvider => ActivatorUtilities.CreateInstance<ServerManager>(serviceProvider, this.loadSaveFile));
        });

        this.host = builder.Build();

        // Register endpoints.
        this.host.MapEndpoint<ServerCore.Hubs.Chat>();
        this.host.MapEndpoint<ServerCore.Hubs.Factory>();
        this.host.MapEndpoint<ServerCore.Hubs.GameHistory>();
        this.host.MapEndpoint<ServerCore.Hubs.Logistics>();
        this.host.MapEndpoint<ServerCore.Hubs.Planet>();
        this.host.MapEndpoint<ServerCore.Hubs.Players>();
        this.host.MapEndpoint<ServerCore.Hubs.Routers>();
        this.host.MapEndpoint<ServerCore.Hubs.Session>();
        this.host.MapEndpoint<ServerCore.Hubs.Statistics>();
        this.host.MapEndpoint<ServerCore.Hubs.Trash>();
        this.host.MapEndpoint<ServerCore.Hubs.Universe>();
        this.host.MapEndpoint<ServerCore.Hubs.Warning>();
        // Catch all Hub
        this.host.MapEndpoint<ServerCore.Hubs.Internal.GenericHub>();

        // Start Client
        this.host.Start();

        this.genericHub = host.Services.GetRequiredService<ServerCore.Hubs.Internal.GenericHub>();
        this.genericHubProxy = host.Services.GetRequiredService<ServerCore.Hubs.Internal.GenericHubProxy>();

        this.genericHub.Initialise();

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
        var builder = new HubConnectionBuilder();

        builder.AddNewtonsoftJsonProtocol();
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
