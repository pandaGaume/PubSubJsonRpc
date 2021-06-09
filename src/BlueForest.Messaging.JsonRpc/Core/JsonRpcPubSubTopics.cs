namespace BlueForest.Messaging.JsonRpc
{
    public class JsonRpcPubSubTopics
    {
        IRpcTopic _request, _response, _notification;

        public JsonRpcPubSubTopics() { }

        public JsonRpcPubSubTopics(IRpcTopic request, IRpcTopic response, IRpcTopic notification) 
        {
            _request = request;
            _response = response;
            _notification = notification;
        }

        public IRpcTopic Request { get => _request; set => _request = value; }
        public IRpcTopic Response { get => _response; set => _response = value; }
        public IRpcTopic Notification { get => _notification; set => _notification = value; }
    }

}
