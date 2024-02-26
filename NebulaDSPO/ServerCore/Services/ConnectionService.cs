using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using EasyR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NebulaAPI;
using NebulaModel;
using NebulaModel.Utils;
using NebulaWorld;

namespace NebulaDSPO.ServerCore.Services;

internal class ConnectionService : IHostedService, IDisposable
{
    private bool _disposedValue;

    private readonly HubConnection connection;
    private readonly ILogger<ConnectionService> logger;

    private List<IDisposable> endpointSubscriptions = new List<IDisposable>();

    private bool stopRequested = false;
    private bool isConnected = false;
    private readonly ISubject<bool> connectionChangedSubject;

    public bool IsConnected
    {
        get
        {
            return this.isConnected;
        }
        private set
        {
            this.isConnected = value;
            this.logger.LogInformation("Connection State Changed: {State}", value);
        }
    }

    public IObservable<bool> ConectionChanged => this.connectionChangedSubject.AsObservable();

    public ConnectionService(HubConnection connection, ILogger<ConnectionService> logger)
    {
        this.connection = connection;
        this.logger = logger;

        this.connectionChangedSubject = new Subject<bool>();

        connection.Closed += Connection_Closed;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.connection.StartAsync(cancellationToken).ConfigureAwait(false);

        // Authenticate?

        IsConnected = true;
        this.connectionChangedSubject.OnNext(true);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (this.connection == null)
            return;

        stopRequested = true;
        await this.connection.StopAsync(cancellationToken).ConfigureAwait(false);

        // load settings again to dispose the temp soil setting that could have been received from server
        Config.LoadOptions();
        try
        {
            NebulaModAPI.OnMultiplayerSessionChange(false);
            NebulaModAPI.OnMultiplayerGameEnded?.Invoke();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "StopAsync error: {Message}", ex.Message);
        }

        IsConnected = false;
        stopRequested = false;
        this.connectionChangedSubject.OnNext(false);
    }

    public void RegisterEndpoint(Func<HubConnection, IDisposable> configure)
    {
        this.endpointSubscriptions.Add(configure(this.connection));
    }

    private Task Connection_Closed(Exception? arg)
    {
        UnityDispatchQueue.RunOnMainThread(() =>
        {
            // If the client is Quitting by themselves, we don't have to inform them of the disconnection.
            if (stopRequested)
                return;

            // Opens the pause menu on disconnection to prevent NRE when leaving the game
            if (Multiplayer.Session?.IsGameLoaded ?? false)
            {
                GameMain.instance._paused = true;
            }

            // Lots of logic handled by the response code when closing the connection. We'll have to handle that a different way so if it ever reaches this point
            // assume it's a normal disconnection and show the disconnection message depending on the games current state.
            if (Multiplayer.Session != null && (Multiplayer.Session.IsGameLoaded || Multiplayer.Session.IsInLobby))
            {
                InGamePopup.ShowWarning(
                    "Connection Lost".Translate(),
                    "You have been disconnected from the server.".Translate(),
                    "Quit",
                    Multiplayer.LeaveGame);
                if (!Multiplayer.Session.IsInLobby)
                {
                    return;
                }

                Multiplayer.ShouldReturnToJoinMenu = false;
                Multiplayer.Session.IsInLobby = false;
                UIRoot.instance.galaxySelect.CancelSelect();
            }
            else
            {
                this.logger.LogWarning("Disconnected from server.");
                InGamePopup.ShowWarning(
                    "Server Unavailable".Translate(),
                    "Could not reach the server, please try again later.".Translate(),
                    "OK".Translate(),
                    Multiplayer.LeaveGame);
            }

            return;
        });

        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                foreach (var subscription in this.endpointSubscriptions)
                {
                    subscription.Dispose();
                }

                this.endpointSubscriptions.Clear();
                this.endpointSubscriptions = null!;

                StopAsync().GetAwaiter().GetResult();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ConnectionService()
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
