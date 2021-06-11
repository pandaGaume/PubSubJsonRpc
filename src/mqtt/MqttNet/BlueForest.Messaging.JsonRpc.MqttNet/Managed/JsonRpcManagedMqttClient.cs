using Microsoft.VisualStudio.Threading;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Protocol;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcManagedMqttClient : IMqttClientConnectedHandler, IMqttClientDisconnectedHandler, IMqttApplicationMessageReceivedHandler
    {
        public static TimeSpan AutoReconnectMaxDelayDefault = TimeSpan.FromSeconds(30);

        ManagedBrokerOptions _settings;
        IMqttClient _client;
        SemaphoreSlim _runLock;
        SemaphoreSlim _connectingLock;
        CancellationTokenSource _stoppedSource;
        MqttClientAuthenticateResult _lastConnectionResult;

        ITargetBlock<MqttApplicationMessageReceivedEventArgs> _decoder;
        ITargetBlock<IPublishEvent> _dispatcher;

        ConcurrentDictionary<Guid, JsonRpcSubscription> _subscriptions;

        event AsyncEventHandler<MqttClientConnectedEventArgs> _onConnected;
        internal JsonRpcManagedMqttClient(ManagedBrokerOptions settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _runLock = new SemaphoreSlim(1);
            _connectingLock = new SemaphoreSlim(1);
            _subscriptions = new ConcurrentDictionary<Guid, JsonRpcSubscription>();

            // Configure the pipeline
            var largeBufferOptions = new ExecutionDataflowBlockOptions() { BoundedCapacity = 10000 };

            var decoder = new TransformBlock<MqttApplicationMessageReceivedEventArgs,IPublishEvent>((eventArgs) => {
                var e = new PublishEvent()
                {
                    Payload = new ReadOnlySequence<byte>(eventArgs.ApplicationMessage.Payload),
                    Topic = MqttRpcTopic.Parse(eventArgs.ApplicationMessage.Topic)
                };
#if DEBUG
                var payloadStr = Encoding.UTF8.GetString(e.Payload.ToArray());
                Console.WriteLine($"Receive : {e.Topic} - {payloadStr}");
#endif
                return e;
            }, largeBufferOptions);

            var dispatcher = new ActionBlock<IPublishEvent>(async (e) =>
            {
                var subscriptions = _subscriptions.Values.ToArray();
                foreach(var s in subscriptions.Match(e.Topic))
                {
                    await s.Target.SendAsync(e);
                }
            }, largeBufferOptions);
            
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            decoder.LinkTo(dispatcher, linkOptions);
            _decoder = decoder;
            _dispatcher = dispatcher;

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();
            _client.ConnectedHandler = this;
            _client.ApplicationMessageReceivedHandler = this;
        }
        public async Task<JsonRpcSubscribeResult> SubscribeAsync(ITargetBlock<IPublishEvent> target, IRpcTopic topic, MqttQualityOfServiceLevel qos, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            encoding = encoding ?? Encoding.UTF8;
            var filterBuilder = new MqttTopicFilterBuilder().WithQualityOfServiceLevel(qos).WithTopic(encoding.GetString(topic.Assemble().ToArray()));
            var optionsBuilder = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(filterBuilder.Build());
            MqttClientSubscribeResult results = null;
            results = await _client.SubscribeAsync(optionsBuilder.Build(), cancellationToken);
            var res = results?.Items[0];
            var guid = Guid.Empty;
            if (res.ResultCode < MqttClientSubscribeResultCode.UnspecifiedError)
            {
                guid = Guid.NewGuid();
                int retry = 3;
                while(_subscriptions.ContainsKey(guid) && --retry != 0)
                {
                    guid = Guid.NewGuid();
                }
                var sub = new JsonRpcSubscription(guid, topic, target);
                if (!_subscriptions.TryAdd(guid, sub))
                {
                    // something went wrong - should NEVER append while the keys are ALWAYS unique
                }
            }
            return new JsonRpcSubscribeResult(res.ResultCode, guid);
        }
        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage mess) => _client.PublishAsync(mess);
        public async Task<MqttClientPublishResult> PublishAsync(string topic, ReadOnlySequence<byte> payload, MqttQualityOfServiceLevel qos)
        {
            var builder = new MqttApplicationMessageBuilder().WithPayload(payload.ToArray()).WithTopic(topic).WithQualityOfServiceLevel(qos);
            var mess = builder.Build();
#if DEBUG
            var payloadStr = Encoding.UTF8.GetString(payload.ToArray());
            Console.WriteLine($"send : {topic} - {payloadStr}");
#endif

            return await _client.PublishAsync(mess);
        }
        public event AsyncEventHandler<MqttClientConnectedEventArgs> OnConnected
        {
            add 
            { 
                _onConnected += value;
                if(_client.IsConnected)
                {
                    _ = value.InvokeAsync(this, new MqttClientConnectedEventArgs(_lastConnectionResult));
                }
            }
            remove { _onConnected -= value; }
        }
        public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            _client.ConnectedHandler = null;
            _client.DisconnectedHandler = this;
            await _onConnected?.InvokeAsync(this, eventArgs);
        }
        public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            _client.DisconnectedHandler = null;
            _client.ConnectedHandler = this;
            await TryConnectAsync(_settings, CancellationToken.None);
        }
        public async Task<JsonRpcManagedMqttClient> StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _runLock.WaitAsync(cancellationToken);
                if (_stoppedSource != null)
                {
                    return this;
                }
                 _stoppedSource = new CancellationTokenSource();
            }
            finally
            {
                _runLock.Release();
            }
            _ = Task.Run(async()=> await TryConnectAsync(_settings, cancellationToken));
            return this;
        }
        public async Task<JsonRpcManagedMqttClient> StopAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _runLock.WaitAsync(cancellationToken);
                if (_stoppedSource == null)
                {
                    return this;
                }
                _stoppedSource.Cancel();
            }
            finally
            {
                _runLock.Release();
            }
            _ = Task.Run(async () => await _client?.DisconnectAsync(cancellationToken));
            return this;
        }
        private CancellationToken StoppedToken => _stoppedSource.Token;
        private async Task TryConnectAsync(ManagedBrokerOptions settings, CancellationToken cancellationToken)
        {
            try
            {
                await _connectingLock.WaitAsync(cancellationToken);
               
                if (!_client.IsConnected)
                {
                    try
                    {
                        using (CancellationTokenExtensions.CombinedCancellationToken cts = this.StoppedToken.CombineWith(cancellationToken))
                        {
                            TimeSpan? ttw = null;
                            int retryAttempt = 0;
                            Random jitterer = new Random();
                            do
                            {
                                if (ttw != null)
                                {
                                    await Task.Delay((TimeSpan)ttw, cts.Token);
                                }
                                try
                                {
                                    var o = settings.ClientOptions.BuildMqttClientOptions();
                                    _lastConnectionResult = await this._client.ConnectAsync(o, cts.Token);
                                }
                                catch
                                {
                                }

                                if (!_client.IsConnected && !cts.Token.IsCancellationRequested)
                                {
                                    var maxSeconds = (_settings?.AutoReconnectMaxDelay ?? AutoReconnectMaxDelayDefault).TotalSeconds;

                                    // exponential backoff + jittering 
                                    var seconds = Math.Min(maxSeconds, Math.Pow(2, ++retryAttempt));
                                    ttw = TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(jitterer.Next(0, 100));
                                    continue;
                                }
                                break;

                            } while (true);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
            finally
            {
                _connectingLock.Release();
            }
        }
        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            await _decoder.SendAsync(eventArgs);
        }
    }
}
