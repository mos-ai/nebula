using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Statistics
{
}

internal class StatisticsProxy
{
    private readonly HubConnection connection;

    public StatisticsProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
