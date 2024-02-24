using System.Collections.Generic;
using EasyR.Client;
using NebulaAPI;
using NebulaAPI.GameState;
using NebulaAPI.Networking;
using NebulaDSPO.Hubs.Internal;
using NebulaModel.Networking;
using NebulaModel.Networking.Serialization;
using NebulaModel.Utils;

namespace NebulaDSPO.ServerCore.Hubs.Internal;

/// <summary>
/// <para>A Generic Hub to handle all function that haven't been mapped to a specific hub.</para>
/// <para>Uses the <see cref="NebulaNetPacketProcessor"/> to handle packets as the default implementation does to minimise the work required to initially support updates.</para>
/// </summary>
internal class GenericHub : HubListener
{
    internal INetPacketProcessor PacketProcessor { get; } = new NebulaNetPacketProcessor();

    public override void RegisterEndPoints(HubConnection connection)
    {
        RegisterEndPoint(connection.On<byte[]>("/genericHub/onMessage", OnMessage));
    }

    internal void Initialise()
    {
        foreach (var assembly in AssembliesUtils.GetNebulaAssemblies())
        {
            PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor);
        }

        PacketUtils.RegisterAllPacketProcessorsInCallingAssembly(PacketProcessor as NebulaNetPacketProcessor, false);

        foreach (var assembly in NebulaModAPI.TargetAssemblies)
        {
            PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor);
            PacketUtils.RegisterAllPacketProcessorsInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor, false);
        }
    }

    internal void Update()
        => PacketProcessor.ProcessPacketQueue();

    private void OnMessage(byte[] data)
        => PacketProcessor.EnqueuePacketForProcessing(data, null);
}

internal class GenericHubProxy
{
    private readonly HubConnection connection;

    public GenericHubProxy(HubConnection connection)
    {
        this.connection = connection;
    }

    public Task SendPacketAsync<T>(T packet, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/genericHub/send", packet, cancellationToken);

    public Task SendPacketExcludeAsync<T>(T packet, INebulaConnection exclude, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/genericHub/sendExclude", packet, exclude, cancellationToken);

    public Task SendPacketToLocalPlanetAsync<T>(T packet, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/genericHub/sendToLocalPlanet", packet, cancellationToken);

    public Task SendPacketToLocalStarAsync<T>(T packet, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/genericHub/sendToLocalStar", packet, cancellationToken);

    public Task SendPacketToPlanetAsync<T>(T packet, int planetId, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/genericHub/sendToPlanet", packet, planetId, cancellationToken);

    public Task SendPacketToStarAsync<T>(T packet, int starId, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/genericHub/sendToStar", packet, starId, cancellationToken);

    public Task SendPacketToStarExcludeAsync<T>(T packet, int starId, INebulaConnection exclude, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/genericHub/sendToStarExclusive", packet, starId, exclude, cancellationToken);

    public Task SendToMatchingAsync<T>(T packet, Predicate<INebulaPlayer> condition, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("/genericHub/sendToMatching", packet, condition, cancellationToken);

    public Task SendToPlayersAsync<T>(IEnumerable<KeyValuePair<INebulaConnection, INebulaPlayer>> players, T packet, CancellationToken cancellationToken = default) where T : class, new()
        => this.connection.InvokeAsync("", players, packet, cancellationToken);
}
