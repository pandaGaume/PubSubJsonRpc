using MQTTnet.Client.Publishing;
using MQTTnet.Protocol;
using StreamJsonRpc.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public abstract class MqttJsonRpcServiceV2<T> : IDisposable
        where T : class
    {
        public static int DefaultQos = (int)MqttQualityOfServiceLevel.AtLeastOnce;


        MqttJsonRpcOptions _options; 
        internal T _delegate;
        internal JsonRpcPubSubBlock _target;
        internal JsonRpcPubSubTopics _topics;
        private bool disposedValue;

        public string Namespace => _options.Route.Namespace;
        public string Name => _options.Session.Name;
        public ITargetBlock<IPublishEvent> Target => _target;
        public T Delegate => _delegate;
        public JsonRpcManagedMqttClient Broker => _options.MqttClient;
        public BrokerSession Session => _options.Session;
        public BrokerRoute Route => _options.Route;

        public async Task StartAsync(MqttJsonRpcOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // make sure our complete call gets propagated throughout the whole pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            var controlSession = Session;
            var controlClient = Broker;
            if (controlClient != null)
            {
                _topics = GetTopics(_options);

                var overallQos = (MqttQualityOfServiceLevel)Math.Min(Route.Qos ?? DefaultQos, (int)MqttQualityOfServiceLevel.ExactlyOnce);

                var target = new JsonRpcPubSubBlock(_topics);

                var mqttPublisher = new ActionBlock<IPublishEvent>(async e =>
                {
                    try
                    {
                        var topic = e.Topic.Assemble(Encoding.UTF8);
                        var result = await controlClient.PublishAsync(topic, e.Payload, overallQos);
                        if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                        {
                            ReturnLocalError(e);
                        }
                    }
                    catch
                    {
                        ReturnLocalError(e);
                    }
                });

                target.LinkTo(mqttPublisher, linkOptions);
                _target = target;

                controlClient.OnConnected += async (o, args) =>
                {
                    foreach (var s in Subscriptions())
                    {
                        var result = await controlClient.SubscribeAsync(target, s, overallQos);
                    }
                };

                OnStarted();
            }
            await options.MqttClient.StartAsync();
        }

        // local loop  response to local rpc client
        private void ReturnLocalError(IPublishEvent e, JsonRpcError.ErrorDetail detail = null)
        {
            var error = new JsonRpcError()
            {
                Error = detail,
                RequestId = e.RequestId
            };
            var mess = new Tuple<JsonRpcMessage, IRpcTopic>(error, new MqttRpcTopic(e.Topic).ReverseInPlace());
            _target.LocalTarget.Post(mess);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _target.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public abstract JsonRpcPubSubTopics GetTopics(MqttJsonRpcOptions options);
        public abstract IEnumerable<IRpcTopic> Subscriptions();
        public abstract void OnStarted();
    }
}
