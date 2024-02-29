using System.Collections.Generic;
using System.Net;
using EasyR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NebulaAPI;
using NebulaDSPO.Hubs.Internal;
using NebulaModel;
using NebulaModel.Packets.Session;
using NebulaModel.Utils;
using NebulaWorld;

namespace NebulaDSPO.Services;

internal class ConnectionService : IHostedService, IDisposable
{
    private bool _disposedValue;

    private readonly HubConnection connection;
    private readonly GenericHubProxy genericHubProxy;
    private readonly PlayerConnectionHubProxy playerConnectionHubProxy;
    private readonly ILogger<ConnectionService> logger;

    private List<IDisposable> endpointSubscriptions = new List<IDisposable>();

    private bool stopRequested = false;

    public ConnectionService(HubConnection connection,
                             GenericHubProxy genericHubProxy,
                             PlayerConnectionHubProxy playerConnectionHubProxy,
                             ILogger<ConnectionService> logger)
    {
        this.connection = connection;
        this.genericHubProxy = genericHubProxy;
        this.playerConnectionHubProxy = playerConnectionHubProxy;
        this.logger = logger;

        connection.Closed += Connection_Closed;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.connection.StartAsync(cancellationToken).ConfigureAwait(false);

        // Authenticate?

        //if (Config.Options.RememberLastClientPassword && !string.IsNullOrWhiteSpace(serverPassword))
        //{
        //    Config.Options.LastClientPassword = serverPassword;
        //    Config.SaveOptions();
        //}

        // Login to lobby

        while (this.connection.State == HubConnectionState.Connecting)
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        if (this.connection.State == HubConnectionState.Disconnected)
        {
            this.logger.LogError("Failed to connect to server.");
            return;
        }

        this.logger.LogInformation("Connected, Logging in.");

        // Don't feel like injecting the client into this class for now, when a proper hub is established I'll rework to it.

        await this.playerConnectionHubProxy.PlayerConnectedAsync(cancellationToken).ConfigureAwait(false);

        await this.genericHubProxy.SendPacketAsync(new LobbyRequest(
            CryptoUtils.GetPublicKey(CryptoUtils.GetOrCreateUserCert()),
            !string.IsNullOrWhiteSpace(Config.Options.Nickname) ? Config.Options.Nickname : GameMain.data.account.userName), cancellationToken)
            .ConfigureAwait(false);

        try
        {
            NebulaModAPI.OnMultiplayerSessionChange(true);
            NebulaModAPI.OnMultiplayerGameStarted?.Invoke();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "StartAsync error: {Message}", ex.Message);
        }
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

        stopRequested = false;
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
