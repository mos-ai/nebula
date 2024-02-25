using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Factory
{
}

internal class FactoryProxy
{
    private readonly HubConnection connection;

    public FactoryProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
