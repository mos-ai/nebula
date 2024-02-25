using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Session
{
    public Session(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
    }
}

internal class SessionProxy
{
    private readonly HubConnection connection;

    public SessionProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
