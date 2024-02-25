using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Chat
{
}

internal class ChatProxy
{
    private readonly HubConnection connection;

    public ChatProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
