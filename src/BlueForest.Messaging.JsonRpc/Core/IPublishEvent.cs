using System;
using System.Buffers;

namespace BlueForest.Messaging.JsonRpc
{
    public interface IPublishEvent 
    {
        IRpcTopic Topic { get; }
        ReadOnlySequence<byte> Payload { get; }
    }
}
