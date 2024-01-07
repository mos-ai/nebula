using System;
using System.Buffers;
using System.Buffers.Binary;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using EnumsNET;
using NebulaModelNew.Packets;

namespace NebulaModelNew;
public abstract class Packet : IDisposable
{
    // Nebula Packet Schema:
    //
    // |
    // | Command | Length | Payload |
    // ------------------------------
    // |  XX XX  | XX XX  | ...     |
    // ------------------------------

    private bool _disposedValue;

    public abstract PacketTypeEnum Type { get; }

    public MemoryOwner<byte> Content { get; init; }

    public int Length => Content.Length;

    public Packet()
    {
        Content = MemoryOwner<byte>.Empty;
    }

    public Packet(ReadOnlySequence<byte> input)
    {
        if (input.Length > int.MaxValue)
        {
            throw new IndexOutOfRangeException($"input buffer exceeds packet limit, actual: {input.Length}, maximum: {int.MaxValue}");
        }

        Content = MemoryOwner<byte>.Allocate((int)input.Length);
        input.CopyTo(Content.Memory.Span);
    }

    public Packet(in ReadOnlySpan<byte> input)
    {
        Content = MemoryOwner<byte>.Allocate(input.Length);
        input.CopyTo(Content.Span);
    }

    public void CopyTo(Span<byte> target)
    {
        // Append Header
        BinaryPrimitives.WriteUInt16BigEndian(target[0..], (ushort)Type);
        BinaryPrimitives.WriteInt32BigEndian(target[sizeof(int)..], Length);
        // Append Content
        Content.Span.CopyTo(target[(sizeof(ushort) + sizeof(int))..]);
    }

    public void CopyTo(Memory<byte> target)
    {
        // Append Header
        BinaryPrimitives.WriteUInt16BigEndian(target.Span[0..], (ushort)Type);
        BinaryPrimitives.WriteInt32BigEndian(target.Span[sizeof(int)..], Length);
        // Append Content
        Content.Memory.CopyTo(target[(sizeof(ushort) + sizeof(int))..]);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Content?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public static class PacketExtensions
{
    private static Type[]? PacketTypes;

    public static Packet? CreateInstance(PacketTypeEnum type, in ReadOnlySequence<byte> bytes)
    {
        if (PacketTypes is null)
        {
            var enumValues = EnumsNET.Enums.GetValues<PacketTypeEnum>();
            PacketTypes = new Type[enumValues.Count];
            for (var i = 0; i < enumValues.Count; i++)
            {
                var packetType = enumValues[0].GetAttributes()?.Get<PacketTypeAttribute>();
                if (packetType is null)
                    continue;

                PacketTypes[i] = packetType.Value;
            }
        }

        try
        {
            return Activator.CreateInstance(PacketTypes[(int)type], bytes) as Packet;
        }
        catch (Exception)   // TODO: which exceptions should be handled by the caller?
        {
            return null;
        }
    }
}
