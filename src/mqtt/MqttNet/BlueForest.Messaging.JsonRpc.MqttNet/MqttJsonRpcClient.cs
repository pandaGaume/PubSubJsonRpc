using MQTTnet;
using MQTTnet.Client.Publishing;
using MQTTnet.Protocol;
using StreamJsonRpc.Protocol;
using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    using static StreamJsonRpc.Protocol.JsonRpcError;

    public class MqttJsonRpcClient<T> : IDisposable
        where T:class
    {
        public static int DefaultQos = (int)MqttQualityOfServiceLevel.AtLeastOnce;

        private T _delegate;
        private JsonRpcPubSubBlock _target;
        private bool disposedValue;

        public MqttJsonRpcClient()
        {
        }
        public T Delegate => _delegate;

        public async Task BuildAsync(JsonRpcMqttSettings settings, int routeIndex = 0, CancellationToken cancellationToken = default)
        {
            // make sure our complete call gets propagated throughout the whole pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            var brokerSettings = settings.Clients;
            if (settings.ProcedureCallSession.HasValue)
            {
                var controlSession = settings.Sessions[settings.ProcedureCallSession.Value];
                var controlClient = await GetMqttClientAsync(brokerSettings[controlSession.Client], cancellationToken);
                if (controlClient != null)
                {
                    var route = controlSession.Routes[routeIndex];

                    var controlTopic = new MqttRpcTopic(route.Path, settings.Name, route.To);
                    var responseTopic = new MqttRpcTopic(route.Path, route.To, settings.Name);
                    var listenTopic = new MqttRpcTopic(route.Path, route.To, MqttRpcTopic.MULTI_LEVEL_WILDCHAR_STR);
                    
                    var qos = (MqttQualityOfServiceLevel)Math.Min(controlSession.Routes[0].Qos ?? DefaultQos, (int)MqttQualityOfServiceLevel.ExactlyOnce);

                    var onConnected = new Func<Task>(async () => {
                        var topic = listenTopic.Assemble(Encoding.UTF8);
                        var result = await controlClient.SubscribeAsync(topic, qos, cancellationToken);
                    });

                    controlClient.OnConnected += (a,b) => onConnected();

                    var target = new JsonRpcPubSubBlock(controlTopic);

                    var mqttPublisher = new ActionBlock<IPublishEvent>(async e =>
                    {
                        try
                        {
                            var topic = e.Topic.Assemble(Encoding.UTF8);
                            var result = await controlClient.PublishAsync(topic, e.Payload, qos);
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

                    _delegate = target.Attach<T>();
                    _target.StartListening();

                    controlClient.OnMessageReceived += ControlClient_OnMessageReceivedAsync;
                }
            }
        }
        
        // local loop  response to local rpc client
        private void ReturnLocalError(IPublishEvent e, ErrorDetail detail = null)
        {
            var error = new JsonRpcError()
            {
                Error = detail,
                RequestId = e.RequestId
            };
            var mess = new Tuple<JsonRpcMessage, IRpcTopic>(error, new MqttRpcTopic(e.Topic).ReverseInPlace());
            _target.LocalTarget.Post(mess);
        }

        private async Task ControlClient_OnMessageReceivedAsync(object sender, MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            // The SendAsync method will return a Task<bool>, and so will block (asynchronously) until the block accepts/declines the message or the block has faulted
            var pe = new PublishEvent()
            {
                Payload = new ReadOnlySequence<byte>(eventArgs.ApplicationMessage.Payload),
                Topic = MqttRpcTopic.Parse(eventArgs.ApplicationMessage.Topic)
            };
            await _target.SendAsync(pe);
        }

        internal async ValueTask<JsonRpcManagedMqttClient> GetMqttClientAsync(ManagedBrokerSettings settings, CancellationToken token )
        {
            var c = new JsonRpcManagedMqttClient();
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
    }
}
