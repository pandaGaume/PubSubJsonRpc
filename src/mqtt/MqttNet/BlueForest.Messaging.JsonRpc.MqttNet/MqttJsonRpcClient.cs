using System.Collections.Generic;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcClient<T> : MqttJsonRpcServiceV2<T>
        where T:class
    {
        public MqttJsonRpcClient(string @namespace, string name) : base(@namespace, name)
        {
        }

        public override JsonRpcPubSubTopics GetTopics(JsonRpcMqttSettings settings, int routeIndex)
        {
            if (settings.ProcedureCallSession.HasValue)
            {
                var controlSession = settings.Sessions[settings.ProcedureCallSession.Value];
                var route = controlSession.Routes[routeIndex];
                var channels = controlSession.Channels ?? BrokerChannels.Default;

                var requestTopic = new MqttRpcTopic(route.Path, channels.Request ?? BrokerChannels.Default.Request, Namespace, Name, route.To);
                var responseTopic = new MqttRpcTopic(route.Path, channels.Response ?? BrokerChannels.Default.Response, Namespace, route.To, Name);
                var notificationTopic = new MqttRpcTopic(route.Path, channels.Notification ?? BrokerChannels.Default.Notification, Namespace, route.To, MqttRpcTopic.MULTI_LEVEL_WILDCHAR_STR);
                var topics = new JsonRpcPubSubTopics(requestTopic, responseTopic, notificationTopic);
                return topics;
            }
            return null;
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
