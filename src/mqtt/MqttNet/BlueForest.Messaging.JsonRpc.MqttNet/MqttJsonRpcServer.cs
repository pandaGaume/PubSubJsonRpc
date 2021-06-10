using System.Collections.Generic;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcServer<T> : MqttJsonRpcServiceV2<T>
        where T : class
    {
        public MqttJsonRpcServer(string @namespace, string name, T api) : base(@namespace, name)
        {
            _delegate = api;
        }

        public override JsonRpcPubSubTopics GetTopics(JsonRpcMqttSettings settings, int routeIndex)
        {
            if (settings.ProcedureCallSession.HasValue)
            {
                var controlSession = settings.Sessions[settings.ProcedureCallSession.Value];
                var route = controlSession.Routes[routeIndex];
                var channels = controlSession.Channels ?? BrokerChannels.Default;

                var requestTopic = new MqttRpcTopic(route.Path, channels.Request ?? BrokerChannels.Default.Request, Namespace, MqttRpcTopic.SINGLE_LEVEL_WILDCHAR_STR, Name);
                var responseTopic = new MqttRpcTopic(route.Path, channels.Response ?? BrokerChannels.Default.Response, Namespace, route.To, Name);
                var notificationTopic = new MqttRpcTopic(route.Path, channels.Notification ?? BrokerChannels.Default.Notification, Namespace, Name, default);
                var topics = new JsonRpcPubSubTopics(requestTopic, responseTopic, notificationTopic);
                return topics;
            }
            return null;
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
