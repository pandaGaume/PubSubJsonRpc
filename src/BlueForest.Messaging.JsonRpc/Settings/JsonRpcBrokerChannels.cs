namespace BlueForest.Messaging.JsonRpc
{
    public class JsonRpcBrokerChannels
    {
        public string Request { get; set; }
        public string Response { get; set; }
        public string Notification { get; set; }
        public JsonRpcBrokerChannel[] UserChannels { get; set; }
    }

}
