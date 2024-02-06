using EasyR.Client;

namespace NebulaEasyRShim.Hubs.Internal;

internal interface IHubListener
{
    void RegisterEndPoints(HubConnection connection);
}
