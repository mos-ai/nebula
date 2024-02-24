using System;
using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Warning : HubListener
{
    public override void RegisterEndPoints(HubConnection connection)
    {
    }
}

internal class WarningProxy
{
    private readonly HubConnection connection;

    public WarningProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}
