using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using System;
using System.Buffers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlueForest.Messaging.JsonRpc.MQTTnet
{
    public class MqttClientJsonRpcInterface : AbstractPubSubJsonRpc, IMqttApplicationMessageReceivedHandler
    {
        readonly IMqttClient _client;

        public MqttClientJsonRpcInterface(IMqttClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            IApplicationMessage e = BuildEvent(eventArgs);
            FirePublishEvent(e);
            return Task.FromResult(0);
        }

        public override async ValueTask PublishAsync(IRpcTopic topic, ReadOnlySequence<byte> payload, PublishOptions options = null, CancellationToken cancel = default)
        {
            var mess = new MqttApplicationMessage()
            {
                Topic = Encoding.UTF8.GetString(topic.Assemble().ToArray()),
                Payload = payload.ToArray()
            };
            await _client.PublishAsync(mess, cancel);
        }

        public override async ValueTask SubscribeAsync(IRpcTopic topic, SubscribeOptions options = null, CancellationToken cancel = default)
        {
            var filter = new MqttTopicFilter()
            {
                Topic = Encoding.UTF8.GetString(topic.Assemble().ToArray())
            } ;
            var o = new MqttClientSubscribeOptions();
            o.TopicFilters.Add(filter);
            await _client.SubscribeAsync(o,cancel);
        }

        public override async ValueTask UnsubscribeAsync(IRpcTopic topic, CancellationToken cancel = default)
        {
            string filter = Encoding.UTF8.GetString(topic.Assemble().ToArray()) ;
            var o = new MqttClientUnsubscribeOptions();
            o.TopicFilters.Add(filter);
            await _client.UnsubscribeAsync(o, cancel);
        }

        private IApplicationMessage BuildEvent(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            MqttApplicationMessage mess = eventArgs.ApplicationMessage;
            return new ApplicationMessageBase()
            {
                Topic = MqttRpcTopic.Parse(mess.Topic),
                Payload = new ReadOnlySequence<byte>(mess.Payload)
            };
        }
    }
}
