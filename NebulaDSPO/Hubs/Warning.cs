﻿using System;
using EasyR.Client;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class Warning
{
}

internal class WarningProxy
{
    private readonly HubConnection connection;

    public WarningProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}