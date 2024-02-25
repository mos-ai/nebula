using EasyR.Client;
using System.Collections.Generic;
using System;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Logistics
{
}

internal class LogisticsProxy
{
    private readonly HubConnection connection;

    public LogisticsProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
