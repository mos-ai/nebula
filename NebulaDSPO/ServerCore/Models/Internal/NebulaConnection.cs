using NebulaAPI.Networking;
using NebulaDSPO.ServerCore.Hubs.Internal;

namespace NebulaDSPO.ServerCore.Models.Internal;

internal record struct NebulaConnection(int Id)
{
    public static implicit operator NullNebulaConnection(NebulaConnection connection) => new NullNebulaConnection(connection);

    public static explicit operator NebulaConnection(NebulaModel.Networking.NebulaConnection connection) => new NebulaConnection(connection.Id);
}
