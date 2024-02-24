using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Trash : HubListener
{
    public override void RegisterEndPoints(HubConnection connection)
    {
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
