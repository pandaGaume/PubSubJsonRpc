using System;
using System.Collections.Generic;
using System.Text;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcOptions
    {
        public JsonRpcManagedMqttClient MqttClient { get; set; }
        public BrokerSession Session { get; set; }
        public BrokerRoute Route { get; set; }
    }
}
