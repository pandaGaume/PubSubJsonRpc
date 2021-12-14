using System.Collections.Generic;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcServer<T> : MqttJsonRpcService<T> where T : class
    {
        public MqttJsonRpcServer(T api, MqttJsonRpcServiceOptions options) : base(options)
        {
            _delegate = api;
        }
        public override JsonRpcPubSubTopics GetTopics(MqttJsonRpcServiceOptions options)
        {
            var localOptions = options ?? Options;
            var session = localOptions.Session;
            var client = localOptions.MqttClient;
            var route = localOptions.Route;
            var tlogic = localOptions.TopicLogic ?? MqttJsonRpcTopicLogic.Shared;
            var stream = route.Stream ?? tlogic.StreamName ?? MqttJsonRpcTopicLogic.Shared.StreamName;
            var channels = route.Channels ?? tlogic.ChannelNames ?? MqttJsonRpcTopicLogic.Shared.ChannelNames;

            var request = channels.Request ?? (tlogic.ChannelNames ?? MqttJsonRpcTopicLogic.Shared.ChannelNames).Request;
            var response = channels.Response ?? (tlogic.ChannelNames ?? MqttJsonRpcTopicLogic.Shared.ChannelNames).Response;
            var notification = channels.Notification ?? (tlogic.ChannelNames ?? MqttJsonRpcTopicLogic.Shared.ChannelNames).Notification;

            var ns = route.Namespace;
            var name = session.Name;
            var path = route.Path;
            var to = route.To;

            var requestTopic = new MqttJsonRpcTopic(path, stream, request, ns, MqttJsonRpcTopicLogic.SINGLE_LEVEL_WILD_STR, name);
            var responseTopic = new MqttJsonRpcTopic(path, stream, response, ns, to, name);
            var notificationTopic = new MqttJsonRpcTopic(path, stream, notification, ns, name, null);
            var topics = new JsonRpcPubSubTopics(requestTopic, responseTopic, notificationTopic);
            return topics;
        }
        public override IEnumerable<IRpcTopic> Subscriptions(JsonRpcPubSubTopics topics)
        {
            yield return topics.Request;
        }

        public override void OnStarted()
        {
            _target.AddLocalRpcTarget(_delegate);
            _target.StartListening();
        }
    }
}
