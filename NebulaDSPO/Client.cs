using System.Net;
using EasyR.Client;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NebulaAPI.GameState;
using NebulaAPI.Networking;
using NebulaDSPO.Services;
using NebulaModel;
using NebulaModel.Networking;
using NebulaModel.Packets.GameStates;
using NebulaModel.Packets.Players;
using NebulaWorld;
using UnityEngine;

namespace NebulaDSPO;

public class Client : IClient
{
    private bool _disposedValue;

    private const float FRAGEMENT_UPDATE_INTERVAL = 0.1f;
    private const float GAME_STATE_UPDATE_INTERVAL = 1f;
    private const float MECHA_SYNCHONIZATION_INTERVAL = 30f;

    private IHost? host;
    private ILogger<Client>? logger;

    private Hubs.Internal.GenericHub? genericHub;
    private Hubs.Internal.GenericHubProxy? genericHubProxy;

    private float fragmentUpdateTimer;
    private float gameStateUpdateTimer;
    private float mechaSynchonizationTimer;

    public IPEndPoint ServerEndpoint { get; set; }
    public INetPacketProcessor PacketProcessor => Hubs.Internal.GenericHub.PacketProcessor;

    public Client(string url, int port, string password = "")
        : this(new IPEndPoint(Dns.GetHostEntry(url).AddressList[0], port), password)
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Neblua API")]
    public Client(IPEndPoint endpoint, string password = "")
    {
        ServerEndpoint = endpoint;
    }

    public void SendPacket<T>(T packet) where T : class, new()
    {
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        genericHubProxy?.SendPacketAsync(packet).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/send"));
    }

    public void SendPacket(byte[] rawData)
    {
        genericHubProxy?.SendPacketAsync(rawData).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/send"));
    }

    public void SendPacketExclude<T>(T packet, INebulaConnection exclude) where T : class, new()
    {
        throw new NotSupportedException($"Client doesn't support {nameof(SendPacketExclude)}");

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        //genericHubProxy?.SendPacketExcludeAsync(packet, exclude).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendExclude"));
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
        throw new NotSupportedException($"Client doesn't support {nameof(SendPacketToPlanet)}");


        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        //genericHubProxy?.SendPacketToPlanetAsync(packet, planetId).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToPlanet"));
    }

    public void SendPacketToStar<T>(T packet, int starId) where T : class, new()
    {
        throw new NotSupportedException($"Client doesn't support {nameof(SendPacketToStar)}");

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        //genericHubProxy?.SendPacketToStarAsync(packet, starId).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToStar"));
    }

    public void SendPacketToStarExclude<T>(T packet, int starId, INebulaConnection exclude) where T : class, new()
    {
        throw new NotSupportedException($"Client doesn't support {nameof(SendPacketToStarExclude)}");

        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        //genericHubProxy?.SendPacketToStarExcludeAsync(packet, starId, exclude).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToStarExclusive"));
    }

    public void SendToMatching<T>(T packet, Predicate<INebulaPlayer> condition) where T : class, new()
    {
        throw new NotSupportedException($"Client doesn't support {nameof(SendToMatching)}");
        
        // TODO: Implement a solution.
        //HubDispatcher.Dispatch<T>(packet);

        // For now just put all data through the generic hub
        //genericHubProxy?.SendToMatchingAsync(packet, condition).SafeFireAndForget(ex => this.logger?.LogError(ex, "Failed to call /genericHub/sendToMatching"));
    }

    public void Start()
    {
        if (this.host is not null)
            Stop();

        var builder = new HostBuilder();
        builder.ConfigureLogging(logBuilder =>
        {
            logBuilder.AddProvider(new Utilities.Logging.BepInExLoggerProvider());
            logBuilder.SetMinimumLevel(LogLevel.Trace);
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(serviceProvider => CreateSocketConnection(ServerEndpoint));
            services.AddSingleton<ConnectionService>();
            services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ConnectionService>());

            // Hubs
            //services.AddSingleton<Hubs.Chat>();
            //services.AddSingleton<Hubs.Factory>();
            //services.AddSingleton<Hubs.GameHistory>();
            //services.AddSingleton<Hubs.Logistics>();
            //services.AddSingleton<Hubs.Planet>();
            //services.AddSingleton<Hubs.Players>();
            //services.AddSingleton<Hubs.Routers>();
            //services.AddSingleton<Hubs.Session>();
            //services.AddSingleton<Hubs.Statistics>();
            //services.AddSingleton<Hubs.Trash>();
            //services.AddSingleton<Hubs.Universe>();
            //services.AddSingleton<Hubs.Warning>();
            // Catch all Hub
            services.AddSingleton<Hubs.Internal.GenericHub>();

            // Client Proxies
            //services.AddSingleton<Hubs.ChatProxy>();
            //services.AddSingleton<Hubs.FactoryProxy>();
            //services.AddSingleton<Hubs.GameHistoryProxy>();
            //services.AddSingleton<Hubs.LogisticsProxy>();
            //services.AddSingleton<Hubs.PlanetProxy>();
            //services.AddSingleton<Hubs.PlayersProxy>();
            //services.AddSingleton<Hubs.RoutersProxy>();
            //services.AddSingleton<Hubs.SessionProxy>();
            //services.AddSingleton<Hubs.StatisticsProxy>();
            //services.AddSingleton<Hubs.TrashProxy>();
            //services.AddSingleton<Hubs.UniverseProxy>();
            //services.AddSingleton<Hubs.WarningProxy>();
            // Catch all Client Proxy
            services.AddSingleton<Hubs.Internal.GenericHubProxy>();
            services.AddSingleton<Hubs.Internal.PlayerConnectionHubProxy>();
        });

        this.host = builder.Build();

        // Need to ensure initialised before connecting.
        // Need to rework design.
        this.genericHub = host.Services.GetRequiredService<Hubs.Internal.GenericHub>();
        this.genericHubProxy = host.Services.GetRequiredService<Hubs.Internal.GenericHubProxy>();

        // Start Client
        this.host.Start();
    }

    public void Stop()
    {
        this.host?.StopAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false).GetAwaiter().GetResult();
        this.host = null;
        Dispose();
    }

    public void Update()
    {
        this.genericHub?.Update();

        if (Multiplayer.Session.IsGameLoaded)
        {
            mechaSynchonizationTimer += Time.deltaTime;
            if (mechaSynchonizationTimer > MECHA_SYNCHONIZATION_INTERVAL)
            {
                SendPacket(new PlayerMechaData(GameMain.mainPlayer));
                mechaSynchonizationTimer = 0f;
            }

            gameStateUpdateTimer += Time.deltaTime;
            if (gameStateUpdateTimer >= GAME_STATE_UPDATE_INTERVAL)
            {
                if (!GameMain.isFullscreenPaused)
                {
                    SendPacket(new GameStateRequest());
                }

                gameStateUpdateTimer = 0f;
            }
        }

        // TODO: Calculate throughput.

        //fragmentUpdateTimer += Time.deltaTime;
        //if (!(fragmentUpdateTimer >= FRAGEMENT_UPDATE_INTERVAL))
        //{
        //    return;
        //}
        //
        //if (GameStatesManager.FragmentSize > 0)
        //{
        //    GameStatesManager.UpdateBufferLength(GetFragmentBufferLength());
        //}
        //
        //fragmentUpdateTimer = 0f;
    }

    private static HubConnection CreateSocketConnection(IPEndPoint? endpoint = null)
    {
        endpoint ??= new IPEndPoint(IPAddress.Loopback, 9000);
        var builder = new HubConnectionBuilder();

        ((LocalPlayer)Multiplayer.Session.LocalPlayer).IsHost = false;

        if (Config.Options.RememberLastIP)
        {
            // We've successfully connected, set connection as last ip, cutting out "ws://" and "/socket"
            Config.Options.LastIP = endpoint.ToString();
            Config.SaveOptions();
        }

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
