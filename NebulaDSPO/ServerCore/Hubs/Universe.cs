using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Universe : HubListener
{
    public override void RegisterEndPoints(HubConnection connection)
    {
    }
}

internal class UniverseProxy
{
    private readonly HubConnection connection;

    public UniverseProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
