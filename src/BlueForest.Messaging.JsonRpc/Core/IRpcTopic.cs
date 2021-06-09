using System;

namespace BlueForest.Messaging.JsonRpc
{
    public interface IRpcTopic
    {
        ReadOnlyMemory<byte> Path { get; set; }
        ReadOnlyMemory<byte> Channel { get; set; }
        ReadOnlyMemory<byte> From { get; set; }
        ReadOnlyMemory<byte> To { get; set; }
        IRpcTopic ReverseInPlace();
    }
}
