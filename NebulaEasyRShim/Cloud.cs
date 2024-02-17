using System.Net;
using Bedrock.Framework;
using EasyR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NebulaEasyRShim.Hubs.Internal;

namespace NebulaEasyRShim;

public static class Cloud
{
    private static CancellationTokenSource? HostCTS;

    internal static IHost? Host;

    public static HubProxy Server = default;

    public static async Task StartClientAsync()
    {
        var builder = new HostBuilder();

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(serviceProvider => CreateSocketConnection(serviceProvider));

            // Register Hubs
            services.AddSingleton<Hubs.Chat>();
        });

        Host = builder.Build();
        var connection = Host.Services.GetRequiredService<HubConnection>();

        // Register Endpoints.
        connection.MapEndpoint<Hubs.Chat>(Host.Services);

        HostCTS = new CancellationTokenSource();
        Console.WriteLine("Starting Host");
        await Host.StartAsync().ConfigureAwait(false);
        Console.WriteLine("Connecting");
        if (await ConnectAsync(connection, HostCTS.Token).ConfigureAwait(false))
        {
            Console.WriteLine("Connected");
            Server = ActivatorUtilities.CreateInstance<HubProxy>(Host.Services);
        }
        else
        {
            Console.WriteLine("Failed to connect.");
        }
    }

    public static async Task StopClientAsync()
    {
        Console.WriteLine("Stopping Host");
        if (Host is null)
            return;

        HostCTS?.Cancel();
        await Host.StopAsync().ConfigureAwait(false);
        Host.Dispose();
    }

    private static HubConnection CreateNamedPipeConnection(IServiceProvider serviceProvider)
    {
        var endPoint = new NamedPipeEndPoint("dspo", ".", impersonationLevel: System.Security.Principal.TokenImpersonationLevel.None);
        var builder = new HubConnectionBuilder();

        builder.AddNewtonsoftJsonProtocol();
        //builder.AddStructPackProtocol();
        builder.WithNamedPipe(endPoint);
        return builder.Build();
    }

    private static HubConnection CreateSocketConnection(IServiceProvider serviceProvider)
    {
        var endPoint = new IPEndPoint(IPAddress.Loopback, 9000);
        var builder = new HubConnectionBuilder();

        builder.AddNewtonsoftJsonProtocol();
        //builder.AddStructPackProtocol();
        builder.WithSocket(endPoint);
        return builder.Build();
    }

    private static async Task<bool> ConnectAsync(HubConnection connection, CancellationToken token = default)
    {
        var tries = 0;
        while (tries++ < 5)
        {
            try
            {
                Console.WriteLine("Connecting...");
                await connection.StartAsync(token).ConfigureAwait(false);
                return true;
            }
            catch when (token.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Failed to connect, trying again in 5000(ms)");

                await Task.Delay(5000, token).ConfigureAwait(false);
            }
        }

        return false;
    }
}
