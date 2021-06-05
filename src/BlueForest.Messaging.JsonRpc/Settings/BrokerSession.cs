namespace BlueForest.Messaging.JsonRpc
{
    public class BrokerSession
    {
        public int Client { get; set; }
        public BrokerRoute[] Routes { get; set; }
    }
}
