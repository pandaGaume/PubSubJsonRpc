using System;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class ManagedBrokerOptions
    {
        public BrokerOptions ClientOptions { get; set; }
        public TimeSpan? AutoReconnectMaxDelay { get; set; }
        public IRpcTopicLogic TopicLogic{ get; set; }
    }
}