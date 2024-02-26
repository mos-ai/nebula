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
using NebulaModel.Packets;
using NebulaModel.Utils;

using NebulaConnection = NebulaDSPO.ServerCore.Models.Internal.NebulaConnection;

namespace NebulaDSPO.ServerCore.Hubs.Internal;

/// <summary>
/// <para>A Generic Hub to handle all function that haven't been mapped to a specific hub.</para>
/// <para>Uses the <see cref="NebulaNetPacketProcessor"/> to handle packets as the default implementation does to minimise the work required to initially support updates.</para>
/// </summary>
internal class GenericHub
{
    private readonly ILogger<GenericHub> logger;

    internal INetPacketProcessor PacketProcessor { get; } = new NebulaNetPacketProcessor();

    public GenericHub(ConnectionService connection, ILogger<GenericHub> logger)
    {
        this.logger = logger;

        Initialise();
        connection.RegisterEndpoint(ep => ep.On<byte[]>("/serverCore/genericHub/onMessage", OnMessage));
    }

    internal void Initialise()
    {
        foreach (var assembly in AssembliesUtils.GetNebulaAssemblies())
        {
            this.logger.LogTrace("Registering assembly (Nebula): {Assembly.FullName}", assembly.FullName);
            PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor);
        }

        PacketUtils.RegisterAllPacketProcessorsInCallingAssembly(PacketProcessor as NebulaNetPacketProcessor, false);

        foreach (var assembly in NebulaModAPI.TargetAssemblies)
        {
            this.logger.LogTrace("Registering assembly (TargetAssemblies): {Assembly.FullName}", assembly.FullName);
            PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor);
            PacketUtils.RegisterAllPacketProcessorsInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor, false);
        }

        var nebulaNetworkAssembly = Assembly.GetAssembly(typeof(NebulaNetwork.Server));
        this.logger.LogTrace("Registering assembly (Manual): {Assembly.FullName}", nebulaNetworkAssembly.FullName);
        PacketUtils.RegisterAllPacketProcessorsInAssembly(nebulaNetworkAssembly, PacketProcessor as NebulaNetPacketProcessor, false);
    }

    internal void Update()
        => PacketProcessor.ProcessPacketQueue();

    private void OnMessage(byte[] data)
        => PacketProcessor.EnqueuePacketForProcessing(data, null);
}

internal class GenericHubProxy
{
    private readonly HubConnection connection;
    private readonly GenericHub genericHub;

    public GenericHubProxy(HubConnection connection, GenericHub genericHub)
    {
        this.connection = connection;
        this.genericHub = genericHub;
    }

    public Task SendPacketAsync<T>(T packet, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/serverCore/genericHub/send", this.genericHub.PacketProcessor.Write(packet), cancellationToken);

    public Task SendPacketExcludeAsync<T>(T packet, NebulaConnection exclude, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/serverCore/genericHub/sendExclude", this.genericHub.PacketProcessor.Write(packet), exclude, cancellationToken);

    public Task SendToPlayersAsync<T>(IEnumerable<NebulaConnection> players, T packet, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/serverCore/genericHub/sendToPlayers", players.ToList(), this.genericHub.PacketProcessor.Write(packet), cancellationToken);
}
