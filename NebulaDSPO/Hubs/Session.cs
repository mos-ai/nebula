using EasyR.Client;
using System.Collections.Generic;
using System;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Session
{
}

internal class SessionProxy
{
    private readonly HubConnection connection;

    public SessionProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
