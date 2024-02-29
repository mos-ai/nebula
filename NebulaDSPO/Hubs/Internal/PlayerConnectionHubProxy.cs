using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyR.Client;

namespace NebulaDSPO.Hubs.Internal;
internal class PlayerConnectionHubProxy
{
    private readonly HubConnection connection;

    public PlayerConnectionHubProxy(HubConnection connection)
    {
        this.connection = connection;
    }

    public Task PlayerConnectedAsync(CancellationToken cancellationToken = default)
        => this.connection.InvokeAsync("/playerConnectionHub/connected", cancellationToken);
}
