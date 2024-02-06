using Bedrock.Framework;
using EasyR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NebulaEasyRShim.Hubs.Internal;

namespace NebulaEasyRShim;

public static class Cloud
{
    private static CancellationTokenSource? HostCTS;
    private static Task? HostTask;

    internal static IHost? Host;
    
    public static HubProxy Server = default;

    public static void StartClient()
    {
        var builder = new HostBuilder();

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(serviceProvider => CreateConnection(serviceProvider));

            // Register Hubs
            services.AddSingleton<Hubs.Chat>();
        });

        Host = builder.Build();
        var connection = Host.Services.GetRequiredService<HubConnection>();
        // Register Endpoints.
        connection.MapEndpoint<Hubs.Chat>();

        HostCTS = new CancellationTokenSource();
        HostTask = Task.Run(async () => await Host.StartAsync(), HostCTS.Token);
        ConnectAsync(connection, HostCTS.Token).SafeFireAndForget();

        Server = ActivatorUtilities.CreateInstance<HubProxy>(Host.Services);
    }

    public static void StopClient()
    {
        if (Host is null)
            return;

        HostCTS?.Cancel();

        Host.StopAsync().GetAwaiter().GetResult();
    }

    private static HubConnection CreateConnection(IServiceProvider serviceProvider)
    {
        var endPoint = new NamedPipeEndPoint("dspo", ".", impersonationLevel: System.Security.Principal.TokenImpersonationLevel.None);
        var builder = new HubConnectionBuilder();

        builder.AddNewtonsoftJsonProtocol();
        //builder.AddStructPackProtocol();
        builder.WithNamedPipe(endPoint);
        return builder.Build();
    }

    private static async Task<bool> ConnectAsync(HubConnection connection, CancellationToken token = default)
    {
        var tries = 0;
        while (tries++ < 5)
        {
            try
            {
                await connection.StartAsync(token);
                return true;
            }
            catch when (token.IsCancellationRequested)
            {
                return false;
            }
            catch
            {
                Console.WriteLine("Failed to connect, trying again in 5000(ms)");

                await Task.Delay(5000, token);
            }
        }

        return false;
    }
}
