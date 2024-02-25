using EasyR.Client;
using NebulaDSPO.ServerCore.Services;

namespace NebulaDSPO.ServerCore.Hubs;

internal class Planet
{
    public Planet(ConnectionService connection)
    {
        //connection.RegisterEndpoint(ep => ep.On<>("", ));
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
