namespace BlueForest.Messaging.JsonRpc
{
    public class JsonRpcMqttOptions
    {
        public ManagedBrokerOptions[] Clients { get; set; }
        public JsonRpcBrokerSession[] Sessions { get; set; }
        public int? MainSession { get; set; }
    }
}
