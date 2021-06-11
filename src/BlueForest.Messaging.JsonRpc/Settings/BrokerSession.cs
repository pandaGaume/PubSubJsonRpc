namespace BlueForest.Messaging.JsonRpc
{
    public class BrokerSession
    {
        public string Name { get; set; }
        public int Client { get; set; }
        public BrokerChannels Channels { get; set; }
        public BrokerRoute[] Routes { get; set; }
        public int? MainRoute { get; set; }

    }
}
