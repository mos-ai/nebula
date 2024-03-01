using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyR.Client;
using Microsoft.Extensions.Logging;
using NebulaAPI;
using NebulaAPI.Networking;
using NebulaDSPO.ServerCore.Services;
using NebulaModel.Networking;
using NebulaModel.Networking.Serialization;
using NebulaModel.Utils;
using NebulaWorld;

namespace NebulaDSPO.ServerCore.Hubs.Internal;

/// <summary>
/// <para>A Generic Hub to handle all function that haven't been mapped to a specific hub.</para>
/// <para>Uses the <see cref="NebulaNetPacketProcessor"/> to handle packets as the default implementation does to minimise the work required to initially support updates.</para>
/// </summary>
internal class GenericHub
{
    private readonly HubConnection connection;
    private readonly ILogger<GenericHub> logger;

    internal INetPacketProcessor PacketProcessor { get; } = new NebulaNetPacketProcessor();

    public GenericHub(ConnectionService connectionService, HubConnection hubConnection, ILogger<GenericHub> logger)
    {
        this.connection = hubConnection;
        this.logger = logger;

        Initialise();
        connectionService.RegisterEndpoint(ep => ep.On<byte[], string>("/genericHub/onMessage", OnMessage));

        connectionService.RegisterEndpoint(ep => ep.On<byte[], int>("/serverCore/genericHub/onPlanetMessage", OnPlanetMessage));
        connectionService.RegisterEndpoint(ep => ep.On<byte[], int>("/serverCore/genericHub/onStarMessage", OnStarMessage));
    }

    internal void Initialise()
    {
        foreach (var assembly in AssembliesUtils.GetNebulaAssemblies())
        {
            this.logger.LogTrace("Registering assembly (Nebula): {Assembly.FullName}", assembly.FullName);
            PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor);
        }

        PacketUtils.RegisterAllPacketProcessorsInCallingAssembly(PacketProcessor as NebulaNetPacketProcessor, true);

        var nebulaNetworkAssembly = Assembly.GetAssembly(typeof(NebulaNetwork.Server));
        this.logger.LogTrace("Registering assembly (Manual): {Assembly.FullName}", nebulaNetworkAssembly.FullName);
        PacketUtils.RegisterAllPacketProcessorsInAssembly(nebulaNetworkAssembly, PacketProcessor as NebulaNetPacketProcessor, true);

        foreach (var assembly in NebulaModAPI.TargetAssemblies)
        {
            this.logger.LogTrace("Registering assembly (TargetAssemblies): {Assembly.FullName}", assembly.FullName);
            PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor);
            PacketUtils.RegisterAllPacketProcessorsInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor, true);
        }
    }

    internal void Update()
        => PacketProcessor.ProcessPacketQueue();

    private void OnMessage(byte[] data, string connectionId)
    {
        this.logger.LogInformation("Message Received: {ConnectionId}", connectionId);
        if (!((Server)Multiplayer.Session.Server).PlayerConnections.TryGetValue(connectionId, out var connection))
        {
            this.logger.LogInformation("Message Received: Player Not Found");
            this.logger.LogInformation("Clients: {ClientIds}", string.Join(", ", ((Server)Multiplayer.Session.Server).PlayerConnections.Select(x => x.Key)));
            return;
        }

        this.logger.LogInformation("Message Received: Player {PlayerId}, {ConnectionStatus}", connection.Id, connection.ConnectionStatus);
        PacketProcessor.EnqueuePacketForProcessing(data, connection);
    }

    private Task OnPlanetMessage(byte[] data, int planetId)
        => ((Server)Multiplayer.Session.Server).SendPacketToPlanetAsync(data, planetId);

    private Task OnStarMessage(byte[] data, int starId)
        => ((Server)Multiplayer.Session.Server).SendPacketToStarAsync(data, starId);
}

internal class GenericHubProxy
{
    private readonly HubConnection connection;
    private readonly GenericHub genericHub;
    private readonly ILogger<GenericHubProxy> logger;

    public GenericHubProxy(HubConnection connection, GenericHub genericHub, ILogger<GenericHubProxy> logger)
    {
        this.connection = connection;
        this.genericHub = genericHub;
        this.logger = logger;
    }

    public Task SendPacketAsync<T>(T packet, CancellationToken cancellationToken = default) where T : class, new()
    {
        if (typeof(T).FullName != "NebulaModel.Packets.Players.PlayerMovement")
            this.logger.LogInformation("Sending Packet: {PacketType}", typeof(T).FullName);

        return this.connection.SendAsync("/serverCore/genericHub/send", this.genericHub.PacketProcessor.Write(packet), cancellationToken);
    }

    public Task SendPacketExcludeAsync<T>(T packet, INebulaConnection exclude, CancellationToken cancellationToken = default) where T : class, new()
    {
        if (typeof(T).FullName != "NebulaModel.Packets.Players.PlayerMovement")
            this.logger.LogInformation("Sending Packet Exclude: {PacketType}", typeof(T).FullName);

        return this.connection.SendAsync("/serverCore/genericHub/sendExclude", this.genericHub.PacketProcessor.Write(packet), exclude.Id, cancellationToken);
    }

    public Task SendToPlayersAsync<T>(IEnumerable<INebulaConnection> players, T packet, CancellationToken cancellationToken = default) where T : class, new()
    {
        if (typeof(T).FullName != "NebulaModel.Packets.Players.PlayerMovement")
            this.logger.LogInformation("Sending Packet Players: {PacketType}", typeof(T).FullName);

        return this.connection.SendAsync("/serverCore/genericHub/sendToPlayers", players.Select(player => player.Id).ToList(), this.genericHub.PacketProcessor.Write(packet), cancellationToken);
    }

    public Task SendToPlayersAsync(IEnumerable<INebulaConnection> players, byte[] rawData, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Sending Packet: rawData");
        return this.connection.SendAsync("/serverCore/genericHub/sendToPlayers", players.Select(player => player.Id).ToList(), rawData, cancellationToken);
    }
}
