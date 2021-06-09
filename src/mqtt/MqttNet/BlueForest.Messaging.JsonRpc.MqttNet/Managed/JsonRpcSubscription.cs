using System;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcSubscription
    {
        internal readonly Guid _guid;
        internal readonly IRpcTopic _topic;
        internal readonly ITargetBlock<IPublishEvent> _target;

        public JsonRpcSubscription(IRpcTopic topic, ITargetBlock<IPublishEvent> target) : this(Guid.NewGuid(), topic, target)
        {
        }

        public JsonRpcSubscription(Guid guid, IRpcTopic topic, ITargetBlock<IPublishEvent> target)
        {
            _guid = guid;
            _target = target;
            _topic = topic;
        }

        public Guid Identifier => _guid;
        public ITargetBlock<IPublishEvent> Target => _target;
        public IRpcTopic Topic => _topic;
    }
}
