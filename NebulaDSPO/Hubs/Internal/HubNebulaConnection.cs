using NebulaAPI.Networking;
using NebulaModel.Networking.Serialization;
using NebulaWorld;

namespace NebulaDSPO.Hubs.Internal;

internal class HubNebulaConnection : NebulaModel.Networking.NebulaConnection
{
    public override bool IsAlive => true;

    public HubNebulaConnection(int id, INetPacketProcessor packetProcessor)
        : base(null, ((Client)Multiplayer.Session.Client).ServerEndpoint, packetProcessor)
    {
        Id = id;
    }

    public HubNebulaConnection(INebulaConnection connection)
        : base(null, null, null)
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

        return obj.GetType() == GetType() && ((INebulaConnection)obj).Id.Equals(Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override void SendPacket<T>(T packet) where T : class
    {
        Multiplayer.Session.Client.SendPacket(packet);
    }

    public override void SendRawPacket(byte[] rawData)
    {
        ((Client)Multiplayer.Session.Client).SendPacket(rawData);
    }
}
