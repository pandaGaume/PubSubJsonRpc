using BlueForest.Messaging.MqttNet;
using MQTTnet;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Protocol;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        public IManagedMqttClient Broker => _options.MqttClient;
        public JsonRpcBrokerSession Session => _options.Session;
        public JsonRpcBrokerRoute Route => _options.Route;

        public async Task StartAsync(MqttJsonRpcServiceOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            
            // ensure topic logic is initialized
            _options.TopicLogic = _options.TopicLogic ?? DefaultTopicLogic.Shared;

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

                        var tl = _options.TopicLogic ?? DefaultTopicLogic.Shared;
                        var publishTopic = tl.Assemble(e.Topic, TopicUse.Publish);
                        var p = e.Payload.ToArray();
#if DEBUG
                        Console.WriteLine($"Send message, topic='{publishTopic}'");
                        Console.WriteLine($"              payload='{Encoding.UTF8.GetString(p)}'");
#endif
                        var messBuilder = new MqttApplicationMessageBuilder().WithPayload(p).WithTopic(publishTopic).WithQualityOfServiceLevel(overallQos);
                        var publishResult = await client.Client.PublishAsync(messBuilder.Build(), CancellationToken.None);
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
                    var optionsBuilder = new MqttClientSubscribeOptionsBuilder();
                    var tl = _options.TopicLogic ?? DefaultTopicLogic.Shared;
                    foreach (var s in Subscriptions(_target.Options.Topics))
                    {
                        var filterBuilder = new MqttTopicFilterBuilder().WithQualityOfServiceLevel(overallQos).WithTopic(tl.Assemble(s, TopicUse.Subscribe));
                        optionsBuilder.WithTopicFilter(filterBuilder);
                    }
                    var subscribeResult = await client.Client.SubscribeAsync(optionsBuilder.Build(), CancellationToken.None);
                };

                client.OnMessage += OnMessageAsync;
                OnStarted();
            }
            await options.MqttClient.StartAsync();
        }

        private async Task OnMessageAsync(object sender, MqttApplicationMessageReceivedEventArgs args)
        {
            try
            {
                var tl = _options.TopicLogic ?? DefaultTopicLogic.Shared;
                var t = tl.Parse(args.ApplicationMessage.Topic);
                foreach (var s in Subscriptions(_target.Options.Topics).Where(s => tl.Match(s,t)))
                {
#if DEBUG
                    Console.WriteLine($"Received message, topic='{args.ApplicationMessage.Topic}'");
                    Console.WriteLine($"                  payload='{Encoding.UTF8.GetString(args.ApplicationMessage.Payload)}'");
#endif
                    var e = new PublishEvent()
                    {
                        Payload = new ReadOnlySequence<byte>(args.ApplicationMessage.Payload),
                        Topic = t
                    };
                    await _target.SendAsync(e);
                }
            }
            catch (FormatException)
            {
            }
            catch (Exception)
            {
            }
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
