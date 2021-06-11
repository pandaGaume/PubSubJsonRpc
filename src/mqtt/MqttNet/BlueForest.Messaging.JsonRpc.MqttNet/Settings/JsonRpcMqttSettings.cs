namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcMqttSettings
    {
        public ManagedBrokerOptions[] Clients { get; set; }
        public BrokerSession[] Sessions { get; set; }

        public int ? MainSession { get; set; }
    }
}
