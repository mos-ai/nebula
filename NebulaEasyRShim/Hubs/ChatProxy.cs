using System.Threading.Tasks;
using EasyR.Client;

namespace NebulaEasyRShim.Hubs;

public class ChatProxy
{
    private readonly HubConnection connection;

    public ChatProxy(HubConnection connection)
    {
        this.connection = connection;
    }

    public Task Whisper(string sender, string recipient, string message) => connection.InvokeAsync("/chat/whisper", sender, recipient, message);
}
