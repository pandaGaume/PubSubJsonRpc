namespace BlueForest.Messaging.JsonRpc
{
    public class BrokerSession
    {
        public int Client { get; set; }
        public BrokerChannels Channels { get; set; }
        public BrokerRoute[] Routes { get; set; }
    }
}
