using System.Net;
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

internal class ConnectionService : IHostedService
{
    private readonly HubConnection connection;
    private readonly ILogger<ConnectionService> logger;

    private bool stopRequested = false;
    private readonly ISubject<bool> connectionChangedSubject;

    public bool IsConnected { get; private set; }

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

        IsConnected = true;
        this.connectionChangedSubject.OnNext(true);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
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
}
