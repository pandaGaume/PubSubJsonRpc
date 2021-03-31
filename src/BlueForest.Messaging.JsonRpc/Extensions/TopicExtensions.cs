namespace BlueForest.Messaging.JsonRpc
{
    using System.Buffers;

    public static class TopicExtensions
    {
        public static ReadOnlySequence<byte> Assemble(this IRpcTopic topic)
        {
            var first = new TopicSegment(topic.Path);
            var last = first.Append(topic.From).Append(topic.To);
            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }
    }
}
