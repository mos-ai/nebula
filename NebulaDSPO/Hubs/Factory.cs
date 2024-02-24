using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Factory : HubListener
{
    public override void RegisterEndPoints(HubConnection connection)
    {
    }
}

internal class FactoryProxy
{
    private readonly HubConnection connection;

    public FactoryProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
