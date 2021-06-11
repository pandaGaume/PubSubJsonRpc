using System.Collections.Generic;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcClient<T> : MqttJsonRpcServiceV2<T>
        where T:class
    {

        public override JsonRpcPubSubTopics GetTopics(MqttJsonRpcOptions options)
        {
            var session = options.Session;
            var channels = session.Channels ?? BrokerChannels.Default;

            var route = options.Route;
            var ns = route.Namespace;
            var name = session.Name;
            var path = route.Path;
            var to = route.To;
            var requestTopic = new MqttRpcTopic(path, channels.Request ?? BrokerChannels.Default.Request, ns, name, to);
            var responseTopic = new MqttRpcTopic(path, channels.Response ?? BrokerChannels.Default.Response, ns, to, name);
            var notificationTopic = new MqttRpcTopic(path, channels.Notification ?? BrokerChannels.Default.Notification, ns, to, MqttRpcTopic.MULTI_LEVEL_WILDCHAR_STR);
            var topics = new JsonRpcPubSubTopics(requestTopic, responseTopic, notificationTopic);
            return topics;
        }

        public override IEnumerable<IRpcTopic> Subscriptions()
        {
            yield return _topics.Response;
            yield return _topics.Notification;
        }
        public override void OnStarted()
        {
            _delegate = _target.Attach<T>();
            _target.StartListening();
        }
    }
}
