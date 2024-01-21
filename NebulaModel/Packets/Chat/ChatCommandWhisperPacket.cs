using System.Buffers;

namespace NebulaModel.Packets.Chat;

[StructPacker.Pack]
public struct ChatCommandWhisperPacket
{
    public ChatCommandWhisperPacket() { }

    public ChatCommandWhisperPacket(string sender, string recipient, string message)
    {
        SenderUsername = sender;
        RecipientUsername = recipient;
        Message = message;
    }

    public string SenderUsername { get; set; }
    public string RecipientUsername { get; set; }
    public string Message { get; set; }
}

public static partial class Serializers
{
    public static byte[] Serialize(this ChatCommandWhisperPacket source) => source.Pack();
    public static ChatCommandWhisperPacket Deserialize(in byte[] bytes) { var result = new ChatCommandWhisperPacket(); result.Unpack(bytes); return result; }
    public static ChatCommandWhisperPacket Deserialize(in ReadOnlySequence<byte> bytes) { var result = new ChatCommandWhisperPacket(); result.Unpack(bytes.ToArray()); return result; }
}
