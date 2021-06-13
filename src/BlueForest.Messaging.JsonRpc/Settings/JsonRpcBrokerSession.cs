namespace BlueForest.Messaging.JsonRpc
{
    public class JsonRpcBrokerSession
    {
        public string Name { get; set; }
        public int Client { get; set; }
        public JsonRpcBrokerRoute[] Routes { get; set; }
        public int? MainRoute { get; set; }

    }
}
