﻿using System;
using EasyR.Client;
using System.Collections.Generic;
using NebulaDSPO.Hubs.Internal;

namespace NebulaDSPO.Hubs;

internal class GameHistory
{
}

internal class GameHistoryProxy
{
    private readonly HubConnection connection;

    public GameHistoryProxy(HubConnection connection)
    {
        this.connection = connection;
    }
}