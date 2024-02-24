using EasyR.Client;
using System.Collections.Generic;
using System;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Planet : HubListener
{
    public override void RegisterEndPoints(HubConnection connection)
    {
    }
}

internal class PlanetProxy
{
    private readonly HubConnection connection;

    public PlanetProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
