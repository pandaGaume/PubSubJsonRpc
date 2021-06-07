using System;
using System.Collections.Generic;
using System.Text;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class ManagedBrokerSettings
    {
        public BrokerSettings ClientOptions { get; set; }
        public TimeSpan? AutoReconnectMaxDelay { get; set; }
    }
}
