using BlueForest.MqttNet;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcServiceOptions
    {
        public IManagedMqttClient MqttClient { get; set; }
        public IRpcTopicLogic TopicLogic { get; set; }
        public JsonRpcBrokerSession Session { get; set; }
        public JsonRpcBrokerRoute Route { get; set; }
    }
}
