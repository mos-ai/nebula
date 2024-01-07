using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Bedrock.Framework.Protocols;
using CommunityToolkit.HighPerformance.Buffers;
using NebulaModelNew.Packets;

namespace NebulaModelNew;
internal class NebulaProtocol : IMessageReader<Message>, IMessageWriter<Message>
{
    internal static readonly int PayloadPrefixBytes = 2;
    internal static readonly int PayloadCrcBytes = 4;
    internal static readonly int PayloadLengthBytes = 2;

    internal static readonly int PayloadPrefixIndex  = 0;
    internal static readonly int PayloadCrcIndex     = PayloadPrefixIndex + PayloadPrefixBytes;
    internal static readonly int PayloadLengthIndex  = PayloadCrcIndex + PayloadCrcBytes;
    internal static readonly int PayloadStartIndex   = PayloadLengthIndex + PayloadLengthBytes;

    internal static readonly byte[] HeaderStartBytes = [0x4E, 0x42];

    // Nebula Protocol Schema:
    //
    // |
    // | Prefix |     CRC     | Length | Payload |
    // -------------------------------------------
    // |  4E 42 | XX XX XX XX | XX XX  | ...     |
    // -------------------------------------------

    public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out Message message)
    {
        // Not enough data.
        if (input.Length < PayloadStartIndex)
        {
            message = default;
            return false;
        }

        // Locate the start of the first packet.
        var startIndex = input.PositionOf(NebulaProtocol.HeaderStartBytes);
        if (startIndex is null)
        {
            consumed = input.End;
            examined = input.End;
            message = default;
            return false;
        }

        // Check if there is enough data after the startIndex for the header.
        var payload = input.Slice(startIndex.Value);
        if (payload.Length < NebulaProtocol.PayloadStartIndex)
        {
            consumed = startIndex.Value;
            examined = input.End;
            message = default;
            return false;
        }

        // Check if the full payload is available.
        var payloadLength = payload.Slice(NebulaProtocol.PayloadLengthIndex, NebulaProtocol.PayloadLengthBytes);
        var contentLength = payloadLength.IsSingleSegment
            ? BinaryPrimitives.ReadUInt32BigEndian(payloadLength.First.Span)
            : BinaryPrimitives.ReadUInt32BigEndian(payloadLength.ToArray());
        if (payload.Length < contentLength)
        {
            consumed = startIndex.Value;
            examined = input.End;
            message = default;
            return false;
        }

        var messagePayload = payload.Slice(NebulaProtocol.PayloadStartIndex, contentLength);
        message = new Message(messagePayload);
        consumed = messagePayload.End;
        examined = messagePayload.End;
        return true;
    }

    public void WriteMessage(Message message, IBufferWriter<byte> output)
    {
        foreach (var memory in message.Payload)
        {
            output.Write(memory.Span);
        }
    }
}

public struct Message
{
    public ReadOnlySequence<byte> Payload { get; }
    public IList<Packet> Packets { get; }

    public Message(byte[] payload)
        : this(new ReadOnlySequence<byte>(payload))
    {
    }

    public Message(params Packet[] packets)
    {
        var messageSize = NebulaProtocol.PayloadStartIndex + packets.Sum(x => x.Length);
        using var buffer = MemoryOwner<byte>.Allocate(messageSize);
        NebulaProtocol.HeaderStartBytes.CopyTo(buffer.Span[NebulaProtocol.PayloadPrefixIndex..]);
        BinaryPrimitives.WriteInt32BigEndian(buffer.Span[NebulaProtocol.PayloadLengthIndex..], messageSize - NebulaProtocol.PayloadStartIndex);
        var position = 0;
        foreach (var packet in packets)
        {
            packet.CopyTo(buffer.Span.Slice(position, packet.Length));
            position += packet.Length;
        }

        var checksum = CalculateChecksum(buffer.Span);
        BinaryPrimitives.WriteInt32BigEndian(buffer.Span[NebulaProtocol.PayloadCrcIndex..], checksum);
        Payload = new ReadOnlySequence<byte>(buffer.Memory);
        Packets = packets;
    }

    public Message(ReadOnlySequence<byte> payload)
    {
        if (!VerifyChecksum(payload))
        {
            throw new ApplicationException("Invalid Checksum.");
        }

        Payload = payload;
        Packets = new List<Packet>();
        var reader = new SequenceReader<byte>(payload.Slice(NebulaProtocol.PayloadStartIndex));
        while (reader.Remaining > 0)
        {
            var (Consumed, Result) = Parse(reader.UnreadSequence);
            if (Result is not null)
            {
                Packets.Add(Result);
            }

            reader.Advance(Consumed);
        }
    }

    private static (long Consumed, Packet? Result) Parse(in ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsEmpty)
        {
            return (0, null);
        }

        if (sequence.Length < sizeof(ushort) + sizeof(int))
        {
            // No valid payload left in the buffer, advance to the end so the caller knows it's processed.
            return (sequence.Length, null);
        }

        var packetTypeBuffer = sequence.Slice(0, sizeof(ushort));
        var packetType = BinaryPrimitives.ReadUInt16BigEndian(packetTypeBuffer.IsSingleSegment ? packetTypeBuffer.First.Span : packetTypeBuffer.ToArray());
        var packetLengthBuffer = sequence.Slice(sizeof(ushort), sizeof(int));
        var packetLength = BinaryPrimitives.ReadInt32BigEndian(packetLengthBuffer.IsEmpty ? packetLengthBuffer.First.Span : packetLengthBuffer.ToArray());
        var packetLengthIncludingHeader = sizeof(ushort) + sizeof(int) + packetLength;
        // Not enough data for a valid Packet, advance to the end so the caller knows it's processed.
        if (sequence.Length < packetLengthIncludingHeader)
        {
            return (sequence.Length, null);
        }

        var packet = PacketExtensions.CreateInstance(PacketTypeEnum.ChatCommandWhisperPacket, sequence.Slice(0, packetLengthIncludingHeader));
        if (packet is not null)
        {
            return (packetLengthIncludingHeader, packet);
        }

        return (sequence.Length, null);
    }

    private static int CalculateChecksum(in ReadOnlySpan<byte> bytes)
    {
        var checksum = 0;
        foreach (var value in bytes)
        {
            checksum += value;
        }

        return checksum;
    }

    private static int CalculateChecksum(in ReadOnlySequence<byte> bytes)
    {
        if (bytes.IsSingleSegment)
        {
            return CalculateChecksum(bytes.First.Span);
        }

        var reader = new SequenceReader<byte>(bytes);
        var checksum = 0;
        while (reader.Remaining > 0)
        {
            reader.TryRead(out var value);
            checksum += value;
        }

        return checksum;
    }

    private static bool VerifyChecksum(ReadOnlySequence<byte> payload)
    {
        var reader = new SequenceReader<byte>(payload.Slice(NebulaProtocol.PayloadCrcIndex));
        reader.TryReadBigEndian(out var expectedChecksum);
        var checksum = CalculateChecksum(reader.UnreadSequence);

        return checksum == expectedChecksum;
    }
}
