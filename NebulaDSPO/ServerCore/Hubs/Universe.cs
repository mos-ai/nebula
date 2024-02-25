using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Universe
{
    public Universe(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
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
