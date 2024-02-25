using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Routers
{
    public Routers(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
    }
}

internal class RoutersProxy
{
    private readonly HubConnection connection;

    public RoutersProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
