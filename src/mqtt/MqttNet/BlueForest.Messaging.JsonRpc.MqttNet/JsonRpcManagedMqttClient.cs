using Microsoft.VisualStudio.Threading;
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
using System.Threading;
using System.Threading.Tasks;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcManagedMqttClient : IMqttClientConnectedHandler, IMqttClientDisconnectedHandler, IMqttApplicationMessageReceivedHandler
    {
        public static TimeSpan AutoReconnectMaxDelayDefault = TimeSpan.FromSeconds(30);

        ManagedBrokerSettings _settings;
        IMqttClient _client;
        SemaphoreSlim _runLock;
        SemaphoreSlim _connectingLock;
        CancellationTokenSource _stoppedSource;
        MqttClientAuthenticateResult _lastConnectionResult;
        

        event EventHandler<MqttClientConnectedEventArgs> _onConnected;
        event EventHandler<MqttClientDisconnectedEventArgs> _onDisconnected;
        event AsyncEventHandler<MqttApplicationMessageReceivedEventArgs> _onMessageReceived;

        public JsonRpcManagedMqttClient()
        {
            _runLock = new SemaphoreSlim(1);
            _connectingLock = new SemaphoreSlim(1);
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();
            _client.ConnectedHandler = this;
            _client.ApplicationMessageReceivedHandler = this;
        }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(string topic, MqttQualityOfServiceLevel qos, CancellationToken cancellationToken = default)
        {
            var filterBuilder = new MqttTopicFilterBuilder().WithQualityOfServiceLevel(qos).WithTopic(topic);
            var optionsBuilder = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(filterBuilder.Build());
            return await _client.SubscribeAsync(optionsBuilder.Build(), cancellationToken);
        }

        public async Task<MqttClientUnsubscribeResult> UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            var filterBuilder = new MqttTopicFilterBuilder().WithTopic(topic);
            var optionsBuilder = new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(filterBuilder.Build());
            return await _client.UnsubscribeAsync(optionsBuilder.Build(), cancellationToken);
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage mess) => _client.PublishAsync(mess);
        public async Task<MqttClientPublishResult> PublishAsync(string topic, ReadOnlySequence<byte> payload, MqttQualityOfServiceLevel qos)
        {
            var builder = new MqttApplicationMessageBuilder().WithPayload(payload.ToArray()).WithTopic(topic).WithQualityOfServiceLevel(qos);
            var mess = builder.Build();
            return await _client.PublishAsync(mess);
        }

        public event EventHandler<MqttClientConnectedEventArgs> OnConnected
        {
            add 
            { 
                _onConnected += value;
                if(_client.IsConnected)
                {
                    value.Invoke(this, new MqttClientConnectedEventArgs(_lastConnectionResult));
                }
            }
            remove { _onConnected -= value; }
        }

        public event EventHandler<MqttClientDisconnectedEventArgs> OnDisconnected
        {
            add { _onDisconnected += value; }
            remove { _onDisconnected -= value; }
        }

        public event AsyncEventHandler<MqttApplicationMessageReceivedEventArgs> OnMessageReceived
        {
            add { _onMessageReceived += value; }
            remove { _onMessageReceived -= value; }
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            _client.ConnectedHandler = null;
            _client.DisconnectedHandler = this;
            _onConnected?.Invoke(this, eventArgs);
            return Task.CompletedTask;
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            _client.DisconnectedHandler = null;
            _client.ConnectedHandler = this;
            _onDisconnected?.Invoke(this, eventArgs);
            _ = Task.Run(async () =>
            {
                await TryConnectAsync(CancellationToken.None);
            });
            return Task.CompletedTask;
        }

        public async Task<JsonRpcManagedMqttClient> StartAsync(ManagedBrokerSettings settings, CancellationToken cancellationToken = default)
        {
            try
            {
                await _runLock.WaitAsync(cancellationToken);
                if (_stoppedSource != null)
                {
                    return this;
                }
                _settings = settings ?? throw new ArgumentNullException(nameof(settings));
                _stoppedSource = new CancellationTokenSource();
            }
            finally
            {
                _runLock.Release();
            }
            _ = Task.Run(async()=> await TryConnectAsync(cancellationToken));
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

        private async Task TryConnectAsync(CancellationToken cancellationToken)
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
                                    var o = _settings.ClientOptions.BuildMqttClientOptions();
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
            await _onMessageReceived?.InvokeAsync(this, eventArgs);
        }
    }
}
