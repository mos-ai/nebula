using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Chat
{
    public Chat(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
    }
}

internal class ChatProxy
{
    private readonly HubConnection connection;

    public ChatProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
