using System;

namespace NebulaModelNew.Packets;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
sealed class PacketTypeAttribute : Attribute
{
    public Type Value { get; init; }

    public PacketTypeAttribute(Type packetType)
    {
        if (packetType.IsAssignableFrom(typeof(Packet)))
        {
            throw new ArgumentException($"Invalid packet type provided, must derive from {nameof(Packet)}");
        }

        Value = packetType;
    }
}
