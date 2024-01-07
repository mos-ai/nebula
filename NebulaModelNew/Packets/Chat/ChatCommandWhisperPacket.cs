using System.Buffers;
using NebulaModelNew;
using NebulaModelNew.Packets;

namespace NebulaModel.Packets.Chat;

public class ChatCommandWhisperPacket : Packet
{
    public ChatCommandWhisperPacket(ReadOnlySequence<byte> input)
    : base(input)
    {
        // TODO: Parse input.
    }

    public ChatCommandWhisperPacket(string sender, string recipient, string message)
    {
        SenderUsername = sender;
        RecipientUsername = recipient;
        Message = message;
    }

    public string SenderUsername { get; set; }
    public string RecipientUsername { get; set; }
    public string Message { get; set; }
    public override PacketTypeEnum Type => PacketTypeEnum.ChatCommandWhisperPacket;
}
