using EasyR.Client;
using System.Collections.Generic;
using System;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Logistics : HubListener
{
    public override void RegisterEndPoints(HubConnection connection)
    {
    }
}

internal class LogisticsProxy
{
    private readonly HubConnection connection;

    public LogisticsProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
