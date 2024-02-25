using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Logistics
{
    public Logistics(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
    }
}

internal class LogisticsProxy
{
    private readonly HubConnection connection;

    public LogisticsProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
