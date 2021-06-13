using System;

namespace BlueForest.Messaging.JsonRpc
{
    public class JsonRpcPubSubOptions
    {
        public JsonRpcPubSubTopics Topics { get; set; }
        public TimeSpan? RequestTimeout { get; set; }
    }
}
