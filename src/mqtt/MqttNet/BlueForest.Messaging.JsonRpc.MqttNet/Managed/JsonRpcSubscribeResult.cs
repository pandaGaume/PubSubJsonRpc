using MQTTnet.Client.Subscribing;
using System;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcSubscribeResult
    {
        readonly Guid _guid;
        readonly MqttClientSubscribeResultCode _code;

        public JsonRpcSubscribeResult(MqttClientSubscribeResultCode code): this(code, Guid.Empty)
        {
        }

        public JsonRpcSubscribeResult(MqttClientSubscribeResultCode code, Guid guid)
        {
            _code = code;
            _guid = guid;
        }

        public MqttClientSubscribeResultCode Code => _code;
        public Guid Identifier => _guid;
    }
}
