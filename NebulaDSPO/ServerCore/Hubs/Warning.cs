using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Warning
{
    public Warning(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
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
