using EasyR.Client;
using System.Collections.Generic;
using System;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Session : HubListener
{
    public override void RegisterEndPoints(HubConnection connection)
    {
    }
}

internal class SessionProxy
{
    private readonly HubConnection connection;

    public SessionProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
