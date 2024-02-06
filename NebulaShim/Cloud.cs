using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using NebulaModel.Logger;
using NebulaModel.Packets.Chat;
using Protocols;

using NamedPipeEndPoint = Bedrock.Framework.NamedPipeEndPoint;

namespace NebulaShim;
public static class Cloud
{
    private static Microsoft.Extensions.Logging.ILogger? logger;

    private static Process? ServerProcess;
    private static Task? _clientTask;
    private static CancellationTokenSource? cts;

    private static IMessageWriter<Message>? _writerProtocol;
    private static ProtocolWriter? _writer;

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
            UseShellExecute = false,
            CreateNoWindow = true
        };
        ServerProcess = Process.Start(processInfo);

        cts = new CancellationTokenSource();
        _clientTask = Task.Run(async () => await RunClientAsync(cts.Token));
    }

    private static async Task RunClientAsync(CancellationToken token)
    {
        await Task.Delay(2000).ConfigureAwait(false); // Need the client service to finish loading before trying to open the pipe.

        logger?.LogInformation("RunClientAsync Started.");
        logger?.LogInformation("Creating client");
        var client = new ClientBuilder()
            .UseNamedPipes()
            .UseConnectionLogging(logger)
            .Build();

        logger?.LogInformation("Starting connection");
        ConnectionContext? connection = null;
        try
        {
            connection = await client.ConnectAsync(new NamedPipeEndPoint("dspo", ".", impersonationLevel: TokenImpersonationLevel.None), token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Connection exception occured.");
            logger?.LogError($"Message: {ex.Message}");
            logger?.LogError($"Stack Trace:\n{ex.StackTrace}");
        }

        if (connection is null)
        {
            logger?.LogDebug("Connection Failed.");
            logger?.LogDebug($"Cancellation Token: {token.IsCancellationRequested}");
            return;
        }

        logger?.LogInformation($"Client connected to {connection.LocalEndPoint}.");
        var protocol = new LengthPrefixedProtocol();
        _writerProtocol = protocol;
        var reader = connection.CreateReader();
        _writer = connection.CreateWriter();

        logger?.LogInformation("Listening for messages");
        while (!token.IsCancellationRequested)
        {
            try
            {
                var response = await reader.ReadAsync(protocol, token).ConfigureAwait(false);
                if (response.IsCompleted || response.IsCanceled)
                {
                    break;
                }

                HandleMessage(response.Message);
            }
            finally
            {
                reader.Advance();
            }
        }
    }

    public static void StopClient()
    {
        logger?.LogInformation("Shutting down cloud connection.");

        cts?.Cancel();

        ServerProcess?.CloseMainWindow();
        if (!ServerProcess?.WaitForExit(5000) ?? false)
        {
            ServerProcess?.Kill();
        }

        ServerProcess?.Dispose();
        ServerProcess = null;
    }

    public static async ValueTask SendMessageAsync(Message message, CancellationToken token = default)
    {
        logger?.LogInformation("Sending message");
        if (_writer is null || _writerProtocol is null)
        {
            if (_writer is null) logger?.LogDebug("_writer is null.");
            if (_writerProtocol is null) logger?.LogDebug("_writerProtocol is null.");
            throw new NullReferenceException("Not initialised.");
        }

        try
        {
            logger?.LogInformation("Start Write");
            await _writer.WriteAsync(_writerProtocol, message, token).ConfigureAwait(false);
            logger?.LogInformation("Message sent.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Writer exception occured.");
            logger?.LogError($"Message: {ex.Message}");
            logger?.LogError($"Stack Trace:\n{ex.StackTrace}");
        }
    }

    private static void HandleMessage(Message message)
    {
        logger?.LogInformation("Parsing response.");
        var result = Serializers.Deserialize(message.Payload);
        logger?.LogInformation($"Response received: {nameof(ChatCommandWhisperPacketStruct)}, Sender: '{result.SenderUsername}', Recipient: '{result.RecipientUsername}', Message: {result.Message}");
    }
}
