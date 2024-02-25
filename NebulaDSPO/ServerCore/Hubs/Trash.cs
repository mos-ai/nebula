using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Trash
{
    public Trash(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
    }
}

internal class TrashProxy
{
    private readonly HubConnection connection;

    public TrashProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
