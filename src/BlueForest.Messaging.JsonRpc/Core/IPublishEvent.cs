using StreamJsonRpc;
using System;
using System.Buffers;

namespace BlueForest.Messaging.JsonRpc
{
    public enum PublishType
    {
        Unknown,
        Request,
        Response,
        Error,
        Notification
    }

    public interface IPublishEvent
    {
        IRpcTopic Topic { get; }
        ReadOnlySequence<byte> Payload { get; }
        RequestId RequestId { get; set; }
        PublishType PublishType { get;set;}
    }
}
