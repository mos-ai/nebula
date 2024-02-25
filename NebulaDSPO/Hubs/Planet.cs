using EasyR.Client;
using System.Collections.Generic;
using System;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Planet
{
}

internal class PlanetProxy
{
    private readonly HubConnection connection;

    public PlanetProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
