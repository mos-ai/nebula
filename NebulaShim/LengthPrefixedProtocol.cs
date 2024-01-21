using System;
using System.Buffers;
using System.Buffers.Binary;
using Bedrock.Framework.Protocols;

namespace Protocols
{
    public class LengthPrefixedProtocol : IMessageReader<Message>, IMessageWriter<Message>
    {
        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out Message message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndian(out int length) || reader.Remaining < length)
            {
                message = default;
                return false;
            }

            var payload = input.Slice(reader.Position, length);
            message = new Message(payload);

            consumed = payload.End;
            examined = consumed;
            return true;
        }

        public void WriteMessage(Message message, IBufferWriter<byte> output)
        {
            NebulaModel.Logger.Log.Info("Start Write Message");
            try
            {
                var lengthBuffer = output.GetSpan(4);
                BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, (int)message.Payload.Length);
                output.Advance(4);
                foreach (var memory in message.Payload)
                {
                    output.Write(memory.Span);
                }
                NebulaModel.Logger.Log.Info("Finish Write Message");
            }
            catch (Exception ex)
            {
                NebulaModel.Logger.Log.Info("Write Message Exception");
                NebulaModel.Logger.Log.Info($"Message: {ex.Message}");
                NebulaModel.Logger.Log.Info($"Stack Trace\n{ex.StackTrace}");
            }
        }
    }

    public struct Message
    {
        public Message(byte[] payload) : this(new ReadOnlySequence<byte>(payload))
        {

        }

        public Message(ReadOnlySequence<byte> payload)
        {
            Payload = payload;
        }

        public ReadOnlySequence<byte> Payload { get; }
    }
}
