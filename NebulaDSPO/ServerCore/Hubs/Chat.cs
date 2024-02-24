using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Chat : HubListener
{
    public override void RegisterEndPoints(HubConnection connection)
    {
        //RegisterEndPoint(connection.On("", ?));
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
