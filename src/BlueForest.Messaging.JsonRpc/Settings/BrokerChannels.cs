namespace BlueForest.Messaging.JsonRpc
{
    public class BrokerChannels
    {
        public static readonly BrokerChannels Default = new BrokerChannels() { Request = "0", Response = "1", Notification = "2" };

        public string Request { get; set; }
        public string Response { get; set; }
        public string Notification { get; set; }
    }

}
