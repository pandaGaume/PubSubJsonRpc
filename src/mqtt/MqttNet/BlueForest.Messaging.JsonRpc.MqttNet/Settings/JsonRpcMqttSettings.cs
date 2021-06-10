namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcMqttSettings
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public ManagedBrokerSettings[] Clients { get; set; }
        public BrokerSession[] Sessions { get; set; }
        public int? MainSession { get; set; }
        public int? MainRoute { get; set; }
    }
}
