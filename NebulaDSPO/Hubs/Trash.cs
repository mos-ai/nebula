using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Trash
{
}

internal class TrashProxy
{
    private readonly HubConnection connection;

    public TrashProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
