using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Statistics
{
    public Statistics(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
    }
}

internal class StatisticsProxy
{
    private readonly HubConnection connection;

    public StatisticsProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
