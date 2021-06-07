namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcMqttSettings
    {
        public string Name { get; set; }
        public ManagedBrokerSettings[] Clients { get; set; }
        public BrokerSession[] Sessions { get; set; }
        public int? ProcedureCallSession { get; set; }
    }
}
