using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Players
{
    public Players(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
    }
}

internal class PlayersProxy
{
    private readonly HubConnection connection;

    public PlayersProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
