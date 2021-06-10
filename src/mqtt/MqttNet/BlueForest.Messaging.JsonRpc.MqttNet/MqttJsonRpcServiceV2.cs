using MQTTnet;
using MQTTnet.Client.Publishing;
using MQTTnet.Protocol;
using StreamJsonRpc.Protocol;
using System;
using System.Buffers;
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

        internal string _namespace;
        internal string _name;
        internal T _delegate;
        internal JsonRpcPubSubBlock _target;
        internal JsonRpcPubSubTopics _topics;
        private bool disposedValue;

        public MqttJsonRpcServiceV2(string @namespace, string name)
        {
            _namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Namespace => _namespace;
        public string Name => _name;
        public ITargetBlock<IPublishEvent> Target => _target;
        public T Delegate => _delegate;
        public async Task StartAsync(JsonRpcMqttSettings settings, int? sessionIndex = null,int? routeIndex = null, CancellationToken cancellationToken = default)
        {
            // make sure our complete call gets propagated throughout the whole pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            var brokerSettings = settings.Clients;
            sessionIndex = sessionIndex ?? settings.MainSession ?? 0;
            var controlSession = settings.Sessions[(int)sessionIndex];
            var controlClient = await GetMqttClientAsync(brokerSettings[controlSession.Client], cancellationToken);
            if (controlClient != null)
            {
                routeIndex = routeIndex?? settings.MainRoute ?? 0;
                _topics = GetTopics(settings, (int)routeIndex);

                var overallQos = (MqttQualityOfServiceLevel)Math.Min(controlSession.Routes[0].Qos ?? DefaultQos, (int)MqttQualityOfServiceLevel.ExactlyOnce);

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
                        var result = await controlClient.SubscribeAsync(target, s, overallQos, null, cancellationToken);
                    }
                };

                OnStarted();
            }
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

        internal async ValueTask<JsonRpcManagedMqttClientV2> GetMqttClientAsync(ManagedBrokerSettings settings, CancellationToken token )
        {
            var c = new JsonRpcManagedMqttClientV2();
            return await c.StartAsync(settings, token);
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
        public abstract JsonRpcPubSubTopics GetTopics(JsonRpcMqttSettings settings, int routeIndex);
        public abstract IEnumerable<IRpcTopic> Subscriptions();
        public abstract void OnStarted();
    }
}
