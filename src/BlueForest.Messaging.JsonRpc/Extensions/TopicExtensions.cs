namespace BlueForest.Messaging.JsonRpc
{
    using System;
    using System.Buffers;
    using System.Text;

    public static class TopicExtensions
    {
        static ReadOnlyMemory<byte> Separator = new byte[] { (byte)'/' };

        public static ReadOnlySequence<byte> Assemble(this IRpcTopic topic)
        {
            var first = new TopicSegment(topic.Path);
            var last = first.Append(Separator).Append(topic.Channel).Append(Separator).Append(topic.Namespace);
            if (topic.From.Length != 0)
            {
                last = last.Append(Separator).Append(topic.From);
            }
            if (topic.To.Length != 0)
            {
                last = last.Append(Separator).Append(topic.To);
            }
            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }
        public static string Assemble(this IRpcTopic topic, Encoding encoding)=>encoding.GetString(Assemble(topic).ToArray());
    
        public static bool Match(this IRpcTopic topic, IRpcTopic other)
        {
            if (!topic.Channel.Span.SequenceEqual(other.Channel.Span)) return false;

            if (!topic.To.IsEmpty)
            {
                var span = topic.To.Span;
                if (span[0] != MqttRpcTopic.MULTI_LEVEL_WILDCHAR)
                {
                    if (!span.SequenceEqual(other.To.Span)) return false;
                }
            }
            
            if (!topic.From.IsEmpty)
            {
                var span = topic.From.Span;
                if (span[0] != MqttRpcTopic.SINGLE_LEVEL_WILDCHAR)
                {
                    if (!span.SequenceEqual(other.From.Span)) return false;
                }
            }

            if (!topic.Path.Span.SequenceEqual(other.Path.Span)) return false;

            return true;
        }

    }
}
