using NebulaModel.Packets.Chat;

namespace NebulaModelNew.Packets;
public enum PacketTypeEnum : ushort
{
    [PacketType(typeof(ChatCommandWhisperPacket))]
    ChatCommandWhisperPacket,
    [PacketType(typeof(ChatCommandWhoPacket))]
    ChatCommandWhoPacket,
    [PacketType(typeof(NewChatMessagePacket))]
    NewChatMessagePacket,
    [PacketType(typeof(RemoteServerCommandPacket))]
    RemoteServerCommandPacket
}
