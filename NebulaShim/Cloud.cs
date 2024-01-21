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
using NebulaModel.Packets.Chat;
using Protocols;

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
        _writerProtocol = protocol;
        var reader = connection.CreateReader();
        _writer = connection.CreateWriter();

        logger?.LogInformation("Listening for messages");
        while (!token.IsCancellationRequested)
        {
            var response = await reader.ReadAsync(protocol, token).ConfigureAwait(false);
            logger?.LogInformation("Read response.");
            if (response.IsCompleted || response.IsCanceled)
            {
                break;
            }

            reader.Advance();

            HandleMessage(response.Message);
        }
    }

    public static void StopClient()
    {
        cts?.Cancel();

        ServerProcess?.Close();
        ServerProcess = null;
    }

    public static async ValueTask SendMessageAsync(Message message, CancellationToken token = default)
    {
        if (_writer is null || _writerProtocol is null)
        {
            throw new NullReferenceException("Not initialised.");
        }

        await _writer.WriteAsync(_writerProtocol, message, token);
    }

    private static void HandleMessage(Message message)
    {
        var result = Serializers.Deserialize(message.Payload);
        logger?.LogInformation($"Response received: {nameof(ChatCommandWhisperPacket)}, Sender: '{result.SenderUsername}', Recipient: '{result.RecipientUsername}', Message: {result.Message}");
    }
}
