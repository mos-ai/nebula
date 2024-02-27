using System.Reflection;
using EasyR.Client;
using Microsoft.Extensions.Logging;
using NebulaAPI;
using NebulaAPI.Networking;
using NebulaDSPO.Services;
using NebulaModel.Networking;
using NebulaModel.Networking.Serialization;
using NebulaModel.Utils;
using NebulaWorld;

namespace NebulaDSPO.Hubs.Internal;

/// <summary>
/// <para>A Generic Hub to handle all function that haven't been mapped to a specific hub.</para>
/// <para>Uses the <see cref="NebulaNetPacketProcessor"/> to handle packets as the default implementation does to minimise the work required to initially support updates.</para>
/// </summary>
internal class GenericHub
{
    private readonly ILogger<GenericHub> logger;

    internal static INetPacketProcessor PacketProcessor = new NebulaNetPacketProcessor();

    public GenericHub(ConnectionService connection, ILogger<GenericHub> logger)
    {
        this.logger = logger;

        Initialise();
        connection.RegisterEndpoint(ep => ep.On<byte[]>("/genericHub/onMessage", OnMessage));
    }

    internal void Initialise()
    {
        foreach (var assembly in AssembliesUtils.GetNebulaAssemblies())
        {
            this.logger.LogTrace("Registering assembly (Nebula): {Assembly.FullName}", assembly.FullName);
            PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor);
        }

        PacketUtils.RegisterAllPacketProcessorsInCallingAssembly(PacketProcessor as NebulaNetPacketProcessor, false);

        var nebulaNetworkAssembly = Assembly.GetAssembly(typeof(NebulaNetwork.Client));
        this.logger.LogTrace("Registering assembly (Manual): {Assembly.FullName}", nebulaNetworkAssembly.FullName);
        PacketUtils.RegisterAllPacketProcessorsInAssembly(nebulaNetworkAssembly, PacketProcessor as NebulaNetPacketProcessor, false);

        foreach (var assembly in NebulaModAPI.TargetAssemblies)
        {
            this.logger.LogTrace("Registering assembly (TargetAssemblies): {Assembly.FullName}", assembly.FullName);
            PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor);
            PacketUtils.RegisterAllPacketProcessorsInAssembly(assembly, PacketProcessor as NebulaNetPacketProcessor, false);
        }
    }

    internal void Update()
        => PacketProcessor.ProcessPacketQueue();

    private void OnMessage(byte[] rawData)
        => PacketProcessor.EnqueuePacketForProcessing(rawData, null);
}

internal class GenericHubProxy
{
    private readonly HubConnection connection;
    private readonly ILogger<GameHistoryProxy> logger;

    public GenericHubProxy(HubConnection connection, ILogger<GameHistoryProxy> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }

    public Task SendPacketAsync<T>(T packet, CancellationToken cancellationToken = default) where T : class, new()
    {
        this.logger.LogInformation("Sending Packet");
        return this.connection.InvokeAsync("/genericHub/send", GenericHub.PacketProcessor.Write(packet), cancellationToken);
    }

    //public Task SendPacketExcludeAsync<T>(T packet, INebulaConnection exclude, CancellationToken cancellationToken = default) where T : class, new()
    //    => this.connection.InvokeAsync("/genericHub/sendExclude", packet, exclude, cancellationToken);

    public Task SendPacketToLocalPlanetAsync<T>(T packet, CancellationToken cancellationToken = default) where T : class, new()
    {
        this.logger.LogInformation("Sending Packet to Local Planet");
        return this.connection.InvokeAsync("/genericHub/sendToPlanet", GenericHub.PacketProcessor.Write(packet), Multiplayer.Session.LocalPlayer.Data.LocalPlanetId, cancellationToken);
    }

    public Task SendPacketToLocalStarAsync<T>(T packet, CancellationToken cancellationToken = default) where T : class, new()
    {
        this.logger.LogInformation("Sending Packet to Star");
        return this.connection.InvokeAsync("/genericHub/sendToStar", GenericHub.PacketProcessor.Write(packet), Multiplayer.Session.LocalPlayer.Data.LocalStarId, cancellationToken);
    }

    //public Task SendPacketToPlanetAsync<T>(T packet, int planetId, CancellationToken cancellationToken = default) where T : class, new()
    //    => this.connection.InvokeAsync("/genericHub/sendToPlanet", packet, planetId, cancellationToken);

    //public Task SendPacketToStarAsync<T>(T packet, int starId, CancellationToken cancellationToken = default) where T : class, new()
    //    => this.connection.InvokeAsync("/genericHub/sendToStar", packet, starId, cancellationToken);

    //public Task SendPacketToStarExcludeAsync<T>(T packet, int starId, INebulaConnection exclude, CancellationToken cancellationToken = default) where T : class, new()
    //    => this.connection.InvokeAsync("/genericHub/sendToStarExclusive", packet, starId, exclude, cancellationToken);

    //public Task SendToMatchingAsync<T>(T packet, Predicate<INebulaPlayer> condition, CancellationToken cancellationToken = default) where T : class, new()
    //    => this.connection.InvokeAsync("/genericHub/sendToMatching", packet, condition, cancellationToken);
}
