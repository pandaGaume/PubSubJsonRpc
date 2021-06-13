using System;
using System.Buffers;
using System.Text;

namespace BlueForest.Messaging.JsonRpc
{
    public interface IRpcTopicLogic
    {
        string StreamName { get; set; }
        JsonRpcBrokerChannels ChannelNames { get; set; }
        IRpcTopic Parse(string topicStr);
        string Assemble(IRpcTopic topic, TopicUse usage);
        bool Match(IRpcTopic a, IRpcTopic b);
    }
}
