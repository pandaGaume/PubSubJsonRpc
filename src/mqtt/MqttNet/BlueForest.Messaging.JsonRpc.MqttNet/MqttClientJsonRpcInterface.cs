using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Protocol;
using System;
using System.Buffers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlueForest.Messaging.JsonRpc.MQTTnet
{
    public class MqttClientJsonRpcInterface : AbstractPubSubJsonRpc, IMqttApplicationMessageReceivedHandler, IMqttClientConnectedHandler, IMqttClientDisconnectedHandler
    {
        readonly IMqttClient _client;
        readonly IMqttApplicationMessageReceivedHandler _ApplicationMessageReceivedDelegate;
        readonly IMqttClientConnectedHandler _ConnectedHandlerDelegate;
        readonly IMqttClientDisconnectedHandler _DisconnectedHandlerDelegate;
        readonly Func<MqttApplicationMessageReceivedEventArgs, bool> _predicate;

        public MqttClientJsonRpcInterface(IMqttClient client, Func<MqttApplicationMessageReceivedEventArgs, bool> predicate = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _predicate = predicate;
            _ApplicationMessageReceivedDelegate = _client.ApplicationMessageReceivedHandler;
            _ConnectedHandlerDelegate = _client.ConnectedHandler;
            _DisconnectedHandlerDelegate = _client.DisconnectedHandler;
            _client.ApplicationMessageReceivedHandler = this;
            _client.ConnectedHandler = this;
            _client.DisconnectedHandler = this;
        }

        public override bool IsConnected => _client.IsConnected;

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            _ApplicationMessageReceivedDelegate?.HandleApplicationMessageReceivedAsync(eventArgs);
            if (_predicate == null || _predicate(eventArgs))
            {
                IPublishEvent e = BuildPublishEvent(eventArgs);
                OnEvent(e);
            }
            return Task.CompletedTask;
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            _ConnectedHandlerDelegate?.HandleConnectedAsync(eventArgs);
            OnEvent(BuildConnectionEvent(true));
            return Task.CompletedTask;
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            _DisconnectedHandlerDelegate?.HandleDisconnectedAsync(eventArgs);
            OnEvent(BuildConnectionEvent(false));
            return Task.CompletedTask;
        }

        public override async ValueTask<bool> TryPublishAsync(IRpcTopic topic, ReadOnlySequence<byte> payload, PublishOptions options = null, CancellationToken cancel = default)
        {
            var mess = new MqttApplicationMessage()
            {
                Topic = Encoding.UTF8.GetString(topic.Assemble().ToArray()),
                Payload = payload.ToArray(),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce
            };
            try
            {
                var res = await _client.PublishAsync(mess, cancel);
                return res.ReasonCode == MqttClientPublishReasonCode.Success;
            } 
            catch
            {
                return false;
            }
        }

        public override async ValueTask<bool> TrySubscribeAsync(IRpcTopic topic, SubscribeOptions options = null, CancellationToken cancel = default)
        {
            var filter = new MqttTopicFilter()
            {
                Topic = Encoding.UTF8.GetString(topic.Assemble().ToArray())
            };
            var o = new MqttClientSubscribeOptions();
            o.TopicFilters.Add(filter);
            try
            {
                var res = await _client.SubscribeAsync(o, cancel);
                return res.Items[0].ResultCode <= MqttClientSubscribeResultCode.GrantedQoS2;
            }
            catch 
            {
                return false;
            }
        }

        public override async ValueTask<bool> TryUnsubscribeAsync(IRpcTopic topic, CancellationToken cancel = default)
        {
            string filter = Encoding.UTF8.GetString(topic.Assemble().ToArray()) ;
            var o = new MqttClientUnsubscribeOptions();
            o.TopicFilters.Add(filter);
            try
            {
                var res = await _client.UnsubscribeAsync(o, cancel);
                return res.Items[0].ReasonCode <= MqttClientUnsubscribeResultCode.Success;
            }
            catch
            {
                return false;
            }
        }

        private IPublishEvent BuildPublishEvent(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            MqttApplicationMessage mess = eventArgs.ApplicationMessage;
            return new PublishEvent()
            {
                Topic = MqttRpcTopic.Parse(mess.Topic),
                Payload = new ReadOnlySequence<byte>(mess.Payload)
            };
        }
        private IConnectionEvent BuildConnectionEvent(bool status)
        {
            return new ConnectionEvent()
            {
                IsConnected = status
            };
        }
    }
}
