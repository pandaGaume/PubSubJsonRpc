namespace BlueForest.Messaging.JsonRpc
{
    using System.Buffers;
    public class ApplicationMessageBase : IApplicationMessage
    {
        public IRpcTopic Topic { get; set; }

        public ReadOnlySequence<byte> Payload { get; set; }
    }
}
