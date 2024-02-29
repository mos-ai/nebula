using System.Collections.Generic;
using NebulaAPI.Networking;
using NebulaDSPO.ServerCore.Models.Internal;
using NebulaWorld;

namespace NebulaDSPO.ServerCore.Hubs.Internal;

internal class NullNebulaConnection : INebulaConnection
{
    public bool IsAlive { get; }
    public int Id { get; }
    public EConnectionStatus ConnectionStatus { get; set; }

    public NullNebulaConnection(int id)
    {
        Id = id;
    }

    public NullNebulaConnection(NebulaConnection connection)
    {
        Id = connection.Id;
    }

    public bool Equals(INebulaConnection other)
    {
        throw new NotImplementedException();
    }

    public void SendPacket<T>(T packet) where T : class, new()
    {
        Multiplayer.Session.Server.SendToPlayers(
            [new KeyValuePair<INebulaConnection, NebulaAPI.GameState.INebulaPlayer>(this, null!)],
            packet);
    }

    public void SendRawPacket(byte[] rawData)
    {
        ((Server)Multiplayer.Session.Server).SendToPlayersAsync(
            [new KeyValuePair<INebulaConnection, NebulaAPI.GameState.INebulaPlayer>(this, null!)],
            rawData)
            .SafeFireAndForget();
    }

    public static implicit operator NebulaConnection(NullNebulaConnection connection) => new NebulaConnection(connection.Id);
}
