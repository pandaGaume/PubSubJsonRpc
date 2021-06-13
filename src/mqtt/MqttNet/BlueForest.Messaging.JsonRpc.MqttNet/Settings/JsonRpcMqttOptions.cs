namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcMqttOptions
    {
        public ManagedBrokerOptions[] Clients { get; set; }
        public JsonRpcBrokerSession[] Sessions { get; set; }
        public int ? MainSession { get; set; }
    }
}
