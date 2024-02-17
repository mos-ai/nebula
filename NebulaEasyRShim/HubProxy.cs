using System;
using Microsoft.Extensions.DependencyInjection;

namespace NebulaEasyRShim;
public class HubProxy
{
    public Hubs.ChatProxy Chat { get; private set; }

    public HubProxy(IServiceProvider services)
    {
        Chat = ActivatorUtilities.CreateInstance<Hubs.ChatProxy>(services);
    }
}
