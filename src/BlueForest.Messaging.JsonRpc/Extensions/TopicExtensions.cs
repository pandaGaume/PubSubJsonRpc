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
            var last = first.Append(Separator).Append(topic.From).Append(Separator).Append(topic.To);
            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }
        public static string Assemble(this IRpcTopic topic, Encoding encoding)=>encoding.GetString(Assemble(topic).ToArray());
    }
}
