using System.Collections.Generic;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcServer<T> : MqttJsonRpcServiceV2<T>
        where T : class
    {
        public MqttJsonRpcServer(T api) 
        {
            _delegate = api;
        }

        public override JsonRpcPubSubTopics GetTopics(MqttJsonRpcOptions options)
        {
            var session = options.Session;
            var channels = session.Channels ?? BrokerChannels.Default;

            var route = options.Route;
            var ns = route.Namespace;
            var name = session.Name;
            var path = route.Path;
            var to = route.To;

            var requestTopic = new MqttRpcTopic(path, channels.Request ?? BrokerChannels.Default.Request, ns, MqttRpcTopic.SINGLE_LEVEL_WILDCHAR_STR, name);
            var responseTopic = new MqttRpcTopic(path, channels.Response ?? BrokerChannels.Default.Response, ns, to, name);
            var notificationTopic = new MqttRpcTopic(path, channels.Notification ?? BrokerChannels.Default.Notification, ns, name, default);
            var topics = new JsonRpcPubSubTopics(requestTopic, responseTopic, notificationTopic);
            return topics;
        }
        public override IEnumerable<IRpcTopic> Subscriptions()
        {
            yield return _topics.Request;
        }

        public override void OnStarted()
        {
            _target.AddLocalRpcTarget(_delegate);
            _target.RPC.CancelLocallyInvokedMethodsWhenConnectionIsClosed = true;
            _target.StartListening();
        }
    }
}
