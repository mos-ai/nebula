using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class GameHistory
{
    public GameHistory(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
    }
}

internal class GameHistoryProxy
{
    private readonly HubConnection connection;

    public GameHistoryProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
