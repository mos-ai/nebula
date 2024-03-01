using System.Collections.Generic;
using NebulaAPI.Networking;
using NebulaWorld;

namespace NebulaDSPO.ServerCore.Hubs.Internal;

internal class HubNebulaConnection : NebulaModel.Networking.NebulaConnection
{
    public override bool IsAlive => true;

    public HubNebulaConnection(int id, INetPacketProcessor packetProcessor)
        : base(null, ((Server)Multiplayer.Session.Server).ServerEndpoint, packetProcessor)
    {
        Id = id;
    }

    public HubNebulaConnection(INebulaConnection connection, INetPacketProcessor packetProcessor)
        : base(null, ((Server)Multiplayer.Session.Server).ServerEndpoint, packetProcessor)
    {
        Id = connection.Id;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is INebulaConnection connection && connection.Id == Id;
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public override void SendPacket<T>(T packet) where T : class
    {
        Multiplayer.Session.Server.SendToPlayers(
            [new KeyValuePair<INebulaConnection, NebulaAPI.GameState.INebulaPlayer>(this, null!)],
            packet);
    }

    public override void SendRawPacket(byte[] rawData)
    {
        ((Server)Multiplayer.Session.Server).SendToPlayersAsync(
            [new KeyValuePair<INebulaConnection, NebulaAPI.GameState.INebulaPlayer>(this, null!)],
            rawData)
            .SafeFireAndForget();
    }
}
