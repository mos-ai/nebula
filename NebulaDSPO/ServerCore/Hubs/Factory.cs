using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Factory
{
    public Factory(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
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
