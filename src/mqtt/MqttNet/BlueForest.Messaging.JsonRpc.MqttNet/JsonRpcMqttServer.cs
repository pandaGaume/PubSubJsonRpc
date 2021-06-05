using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcMqttServer<T> : IMqttApplicationMessageReceivedHandler, IDisposable
        where T : class
    {
        public static int DefaultQos = (int)MqttQualityOfServiceLevel.AtLeastOnce;

        private T _delegate;
        private IManagedMqttClient[] _clients;
        private JsonRpcPubSubBlock _target;
        private bool disposedValue;

        public JsonRpcMqttServer(T api)
        {
            _delegate = api;
        }
        public T Delegate => _delegate;
        public async Task BuildAsync(JsonRpcMqttSettings settings, int routeIndex = 0)
        {
            // make sure our complete call gets propagated throughout the whole pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            var brokerSettings = settings.Clients;
            if (settings.ProcedureCallSession.HasValue)
            {
                var controlSession = settings.Sessions[settings.ProcedureCallSession.Value];
                var controlClient = await GetMqttClientAsync(controlSession.Client, brokerSettings);
                if (controlClient != null)
                {
                    var route = controlSession.Routes[routeIndex];

                    var notificationTopic = new MqttRpcTopic(route.Path, settings.Name, MqttRpcTopic.MULTI_LEVEL_WILDCHAR_STR);
                    var listenTopic = new MqttRpcTopic(route.Path, MqttRpcTopic.SINGLE_LEVEL_WILDCHAR_STR, settings.Name);

                    var qos = Math.Min(controlSession.Routes[0].Qos ?? DefaultQos, (int)MqttQualityOfServiceLevel.ExactlyOnce);

                    controlClient.UseConnectedHandler(async m =>
                    {
                        var topic = listenTopic.Assemble(Encoding.UTF8);
                        var filter = new MqttTopicFilter() { QualityOfServiceLevel = (MqttQualityOfServiceLevel)qos, Topic = topic };
                        await controlClient.SubscribeAsync(filter);
                    });

                    var target = new JsonRpcPubSubBlock(notificationTopic);

                    var mqttPublisher = new ActionBlock<IPublishEvent>(e =>
                    {
                        var builder = new MqttApplicationMessageBuilder().WithPayload(e.Payload.ToArray()).WithTopic(e.Topic.Assemble(Encoding.UTF8));
                        switch (qos)
                        {
                            case (0): builder.WithAtMostOnceQoS(); break;
                            case (1): builder.WithAtLeastOnceQoS(); break;
                            case (2): builder.WithExactlyOnceQoS(); break;
                        }
                        var m = builder.Build();
                        // All MQTT application messages are added to an internal queue and processed once the server is available.
                        controlClient?.PublishAsync(m);
                    });

                    target.LinkTo(mqttPublisher, linkOptions);

                    _target = target;
                    target.AddLocalRpcTarget(_delegate);
                    _target.StartListening();
                    controlClient.ApplicationMessageReceivedHandler = this;
                }
            }
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            // The SendAsync method will return a Task<bool>, and so will block (asynchronously) until the block accepts/declines the message or the block has faulted
            var e = new PublishEvent()
            {
                Payload = new ReadOnlySequence<byte>(eventArgs.ApplicationMessage.Payload),
                Topic = MqttRpcTopic.Parse(eventArgs.ApplicationMessage.Topic)
            };
            await HandleAPublishEventReceivedAsync(e);
        }

        public virtual async Task HandleAPublishEventReceivedAsync(PublishEvent e)
        {
            if (!await _target.SendAsync(e))
            {
            }
        }

        internal ValueTask<IManagedMqttClient> GetMqttClientAsync(int index, BrokerSettings[] settings)
        {
            _clients = _clients ?? new IManagedMqttClient[settings.Length];
            return settings.GetMqttClientAsync(index, _clients);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var c in _clients)
                    {
                        c.Dispose();
                    }
                    
                    _target.Dispose();

                    if( _delegate is IDisposable d)
                    {
                        d.Dispose();
                    }
                }
                _clients = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
