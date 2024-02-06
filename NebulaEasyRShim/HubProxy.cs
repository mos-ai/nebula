﻿using System;
using Microsoft.Extensions.DependencyInjection;

namespace NebulaEasyRShim;
public class HubProxy
{
    public Hubs.ChatProxy Chat { get; init; }

    public HubProxy(IServiceProvider services)
    {
        Chat = ActivatorUtilities.CreateInstance<Hubs.ChatProxy>(services);
    }
}
