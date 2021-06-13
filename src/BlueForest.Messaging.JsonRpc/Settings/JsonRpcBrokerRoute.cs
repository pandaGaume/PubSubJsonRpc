namespace BlueForest.Messaging.JsonRpc
{
    public class JsonRpcBrokerRoute
    {
        public string Path { get; set; }
        public string Stream { get; set; }
        public JsonRpcBrokerChannels Channels { get; set; }
        public string Namespace { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public int? Qos { get; set; }
    }

}
