using System;
using System.Buffers;

namespace BlueForest.Messaging.JsonRpc
{
    /// <summary>
    /// An utility class to hold topic parts
    /// </summary>
    internal class TopicSegment : ReadOnlySequenceSegment<byte>
    {
        public TopicSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public TopicSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new TopicSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;
            return segment;
        }
    }
}
