namespace BlueForest.Messaging.JsonRpc
{
    using StreamJsonRpc;
    using System.Buffers;
    public class PublishEvent : IPublishEvent
    {
        public IRpcTopic Topic { get; set; }
        public ReadOnlySequence<byte> Payload { get; set; }
        public RequestId RequestId { get; set; }
        public PublishType PublishType { get; set; }
    }
}
