using System.Collections.Generic;
using EasyR.Client;

namespace NebulaDSPO.Hubs.Internal;
internal abstract class HubListener : IHubListener, IDisposable
{
    private bool _disposedValue;

    private List<IDisposable> endpointMappings = new();

    protected void RegisterEndPoint(IDisposable endpoint) => endpointMappings.Add(endpoint);

    public abstract void RegisterEndPoints(HubConnection connection);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                foreach (var mapping in endpointMappings)
                {
                    mapping.Dispose();
                }

                endpointMappings.Clear();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Chat()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
