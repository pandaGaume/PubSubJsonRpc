using System;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcServiceOptions
    {
        public JsonRpcManagedMqttClient MqttClient { get; set; }
        public JsonRpcBrokerSession Session { get; set; }
        public JsonRpcBrokerRoute Route { get; set; }
        public TimeSpan? RequestTimeout { get; set; }
    }
}
