using EasyR.Client;
using System.Collections.Generic;
using System;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Routers
{
}

internal class RoutersProxy
{
    private readonly HubConnection connection;

    public RoutersProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
