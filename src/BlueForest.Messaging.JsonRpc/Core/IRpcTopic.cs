using System;

namespace BlueForest.Messaging.JsonRpc
{
    public enum TopicUse
    {
        Publish, Subscribe
    }

    public interface IRpcTopic 
    {
        string Path { get; set; }
        string Stream { get; set; }
        string Channel { get; set; }
        string Namespace { get; set; }
        string From { get; set; }
        string To { get; set; }
        IRpcTopic ReverseInPlace();
    }
}
