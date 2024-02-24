using EasyR.Client;

namespace NebulaDSPO.Hubs.Internal;

internal interface IHubListener
{
    void RegisterEndPoints(HubConnection connection);
}
