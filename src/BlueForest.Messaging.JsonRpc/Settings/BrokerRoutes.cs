namespace BlueForest.Messaging.JsonRpc
{
    public class BrokerRoute
    {
        public string Path { get; set; }
        public string Namespace { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public int? Qos { get; set; }
    }

}
