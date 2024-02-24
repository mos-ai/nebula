using EasyR.Client;
using System.Collections.Generic;
using System;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Players : HubListener
{
    public override void RegisterEndPoints(HubConnection connection)
    {
    }
}

internal class PlayersProxy
{
    private readonly HubConnection connection;

    public PlayersProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
