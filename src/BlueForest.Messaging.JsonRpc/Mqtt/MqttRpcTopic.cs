namespace BlueForest.Messaging.JsonRpc
{
    using System;

    public class MqttRpcTopic : RpcTopicBase
    {
        public static MqttRpcTopic Parse(string str)
        {
            return default;
        }

        public MqttRpcTopic(ReadOnlyMemory<byte> path, ReadOnlyMemory<byte> from, ReadOnlyMemory<byte> to) : base(path, from, to) { }
    }
}
