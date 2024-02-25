using EasyR.Client;
using System.Collections.Generic;
using System;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Players
{
}

internal class PlayersProxy
{
    private readonly HubConnection connection;

    public PlayersProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
