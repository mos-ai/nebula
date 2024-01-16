using System;
using System.Diagnostics;
using CoreRPC.Routing;
using CoreRPC.Transport.NamedPipe;
using CoreRPC;
using NebulaShim.Grains.Chat;
using DSPO.RPC.Interfaces.Grains.Chat;

namespace NebulaShim;

public static partial class Cloud
{
    private static Process? ServerProcess;

    public static IChatClient? ChatGrain { get; private set; }

    public static void StartClient()
    {
        if (ServerProcess != null)
        {
            StopClient();
        }
        try
        {
            //// From Server Handlers
            //var clientRouter = new DefaultTargetSelector();
            //clientRouter.Register<IChatServer, ChatListener>();
            //var clientEngine = new Engine().CreateRequestHandler(clientRouter);
            //new NamedPipeHost(clientEngine).StartListening("dspo-client");

            //// To Server Handlers
            //var serverTransport = new NamedPipeClientTransport("dspo-server");
            //ChatGrain = new Engine().CreateProxy<IChatClient>(serverTransport);

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
        }
        catch (Exception)
        {
            // Log the error
            throw;
        }
    }

    public static void StopClient()
    {
        // Logout

        ServerProcess?.Close();
        ServerProcess = null;
    }
}
