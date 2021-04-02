namespace BlueForest.Messaging.JsonRpc
{
    using System.Buffers;
    public class PublishEvent : IPublishEvent
    {
        public IRpcTopic Topic { get; set; }

        public ReadOnlySequence<byte> Payload { get; set; }
    }
}
