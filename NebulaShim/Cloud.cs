using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework;
using Bedrock.Framework.Protocols;
using Microsoft.Extensions.Logging;
using NebulaModel.Logger;
using Protocols;

namespace NebulaShim;
public static class Cloud
{
    private static Microsoft.Extensions.Logging.ILogger? logger;

    private static Process? ServerProcess;
    private static Task? _clientTask;
    private static CancellationTokenSource? cts;

    public static void StartClient()
    {
        logger = new NebulaLogger(Log.logger, "Cloud");
        logger.LogInformation("Starting Client");

        StopClient();

        var orleansClientPath = System.IO.Path.Combine(AppContext.BaseDirectory, "BepInEx", "plugins", "nebula-NebulaMultiplayerMod", "DSPO", "net8.0", "DSPO.Client.exe");
        if (!System.IO.File.Exists(orleansClientPath))
        {
            throw new Exception($"Could not find Orleans client at: {orleansClientPath}");
        }

        var processInfo = new ProcessStartInfo(orleansClientPath)
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false
        };
        ServerProcess = Process.Start(processInfo);

        cts = new CancellationTokenSource();
        _clientTask = Task.Run(async () => await RunClientAsync(cts.Token));
    }

    private static async Task RunClientAsync(CancellationToken token)
    {
        await Task.Delay(2000); // Need the client service to finish loading before trying to open the pipe.

        logger?.LogInformation("RunClientAsync Started.");
        cts = new CancellationTokenSource();

        logger?.LogInformation("Creating client");
        var client = new ClientBuilder()
            .UseNamedPipes()
            .UseConnectionLogging(logger)
            .Build();

        logger?.LogInformation("Starting connection");
        var connectCts = new CancellationTokenSource(5000);
        var connection = await client.ConnectAsync(new NamedPipeEndPoint("dspo", impersonationLevel: TokenImpersonationLevel.None), connectCts.Token).ConfigureAwait(false);
        if (connection is null)
        {
            logger?.LogCritical("Connection Failed.");
            logger?.LogCritical($"Timeout: {connectCts.IsCancellationRequested}");
            return;
        }

        logger?.LogInformation($"Client connected to {connection.LocalEndPoint}.");
        var protocol = new LengthPrefixedProtocol();
        var reader = connection.CreateReader();
        var writer = connection.CreateWriter();

        logger?.LogInformation("Starting message loop");
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(2000, token).ConfigureAwait(false);
            logger?.LogInformation("Sending Message...");
            await writer.WriteAsync(protocol, new Message(Encoding.UTF8.GetBytes("IT WORKS!!!!")), token).ConfigureAwait(false);
            var result = await reader.ReadAsync(protocol, token).ConfigureAwait(false);
            logger?.LogInformation("Read response.");
            if (result.IsCompleted)
            {
                break;
            }

            reader.Advance();
        }
    }

    public static void StopClient()
    {
        cts?.Cancel();

        ServerProcess?.Close();
        ServerProcess = null;
    }
}
