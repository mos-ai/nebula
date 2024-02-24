using NebulaDSPO.ServerCore.Hubs.Internal;

namespace NebulaDSPO.ServerCore.Models.Internal;

internal record struct NebulaConnection(int Id)
{
    public static implicit operator NullNebulaConnection(NebulaConnection connection) => new NullNebulaConnection(connection);
}
