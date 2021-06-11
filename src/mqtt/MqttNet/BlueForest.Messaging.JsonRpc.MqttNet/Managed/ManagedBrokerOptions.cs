using System;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class ManagedBrokerOptions
    {
        public BrokerSettings ClientOptions { get; set; }
        public TimeSpan? AutoReconnectMaxDelay { get; set; }
    }
}
