using System.Collections.Generic;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcClient<T> : MqttJsonRpcService<T>
        where T : class
    {

        public override JsonRpcPubSubTopics GetTopics(MqttJsonRpcServiceOptions options)
        {
            var session = options.Session;
            var client = options.MqttClient;
            var route = options.Route;
            var stream = route.Stream ?? client.TopicLogic.StreamName ?? DefaultTopicLogic.Shared.StreamName;
            var channels = route.Channels ?? client.TopicLogic.ChannelNames ?? DefaultTopicLogic.Shared.ChannelNames;

            var request = channels.Request ?? (client.TopicLogic.ChannelNames ?? DefaultTopicLogic.Shared.ChannelNames).Request;
            var response = channels.Response ?? (client.TopicLogic.ChannelNames ?? DefaultTopicLogic.Shared.ChannelNames).Response;
            var notification = channels.Notification ?? (client.TopicLogic.ChannelNames ?? DefaultTopicLogic.Shared.ChannelNames).Notification;

            var ns = route.Namespace;
            var name = session.Name;
            var path = route.Path;
            var to = route.To;

            var requestTopic = new RpcTopic(path, stream, request, ns, name, to);
            var responseTopic = new RpcTopic(path, stream, response, ns, to, name);
            var notificationTopic = new RpcTopic(path, stream, notification, ns, to, DefaultTopicLogic.MULTI_LEVEL_WILD_STR);
            var topics = new JsonRpcPubSubTopics(requestTopic, responseTopic, notificationTopic);
            return topics;
        }

        public override IEnumerable<IRpcTopic> Subscriptions(JsonRpcPubSubTopics topics)
        {
            yield return topics.Response;
            yield return topics.Notification;
        }
        public override void OnStarted()
        {
            _delegate = _target.Attach<T>();
            _target.StartListening();
        }
    }
}
