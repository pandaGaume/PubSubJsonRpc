using MQTTnet.Client.Publishing;
using MQTTnet.Protocol;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public abstract class MqttJsonRpcService<T> : IDisposable
        where T : class
    {
        public static int DefaultQos = (int)MqttQualityOfServiceLevel.AtLeastOnce;

        MqttJsonRpcServiceOptions _options;
        internal T _delegate;
        internal JsonRpcPubSubBlock _target;
        private bool disposedValue;
        public string Namespace => _options.Route.Namespace;
        public string Name => _options.Session.Name;
        public ITargetBlock<IPublishEvent> Target => _target;
        public T Delegate => _delegate;
        public JsonRpcManagedMqttClient Broker => _options.MqttClient;
        public JsonRpcBrokerSession Session => _options.Session;
        public JsonRpcBrokerRoute Route => _options.Route;

        public async Task StartAsync(MqttJsonRpcServiceOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // make sure our complete call gets propagated throughout the whole pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            var session = Session;
            var client = Broker;
            if (client != null)
            {
                var o = new JsonRpcPubSubOptions()
                {
                    Topics = GetTopics(_options)
                };
                var overallQos = (MqttQualityOfServiceLevel)Math.Min(Route.Qos ?? DefaultQos, (int)MqttQualityOfServiceLevel.ExactlyOnce);

                var target = new JsonRpcPubSubBlock(o);

                var mqttPublisher = new ActionBlock<IPublishEvent>(async e =>
                {
                    try
                    {
                        var publishTopic = client.TopicLogic.Assemble(e.Topic, TopicUse.Publish);
                        var publishResult = await client.PublishAsync(publishTopic, e.Payload, overallQos);
                        if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
                        {
                            OnPublishFault(e);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnPublishFault(e, ex);
                    }
                });

                target.LinkTo(mqttPublisher, linkOptions);
                _target = target;

                client.OnConnected += async (o, args) =>
                {
                    foreach (var s in Subscriptions(_target.Options.Topics))
                    {
                        var subscribeResult = await client.SubscribeAsync(target, s, overallQos);
                    }
                };

                OnStarted();
            }
            await options.MqttClient.StartAsync();
        }

        private void OnPublishFault(IPublishEvent publish, Exception ex = null)
        {
            if (publish.PublishType == PublishType.Request)
            {
                _target.RpcTarget.PostBackRequestError(publish.RequestId, JsonRpcErrorCode.InternalError, ex?.Message);
            }
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
        public abstract JsonRpcPubSubTopics GetTopics(MqttJsonRpcServiceOptions options);
        public abstract IEnumerable<IRpcTopic> Subscriptions(JsonRpcPubSubTopics topics);
        public abstract void OnStarted();
    }
}
