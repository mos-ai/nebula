using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Universe
{
}

internal class UniverseProxy
{
    private readonly HubConnection connection;

    public UniverseProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
