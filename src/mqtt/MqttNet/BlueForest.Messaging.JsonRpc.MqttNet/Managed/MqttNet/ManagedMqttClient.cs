using Microsoft.VisualStudio.Threading;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Receiving;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlueForest.Messaging.MqttNet
{
    public class ManagedMqttClient : IManagedMqttClient, IMqttClientConnectedHandler, IMqttClientDisconnectedHandler, IMqttApplicationMessageReceivedHandler
    {
        public static TimeSpan AutoReconnectMaxDelayDefault = TimeSpan.FromSeconds(30);

        ManagedBrokerOptions _settings;
        IMqttClient _client;
        SemaphoreSlim _runLock;
        SemaphoreSlim _connectingLock;
        CancellationTokenSource _stoppedSource;
        MqttClientAuthenticateResult _lastConnectionResult;

        event AsyncEventHandler<MqttClientConnectedEventArgs> _onConnected;
        event AsyncEventHandler<MqttClientDisconnectedEventArgs> _onDisconnected;
        event AsyncEventHandler<MqttApplicationMessageReceivedEventArgs> _onMessage;

        public ManagedMqttClient(ManagedBrokerOptions settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _runLock = new SemaphoreSlim(1);
            _connectingLock = new SemaphoreSlim(1);
            _client = CreateClient();
            _client.ConnectedHandler = this;
            _client.ApplicationMessageReceivedHandler = this;
        }

        protected virtual IMqttClient CreateClient()
        {
            var factory = new MqttFactory();
            return factory.CreateMqttClient();
        }

        #region IManagedMqttClient
        public IMqttClient Client => _client;

        public event AsyncEventHandler<MqttClientConnectedEventArgs> OnConnected
        {
            add
            {
                _onConnected += value;
                if (_client.IsConnected)
                {
                    _ = value.InvokeAsync(this, new MqttClientConnectedEventArgs(_lastConnectionResult));
                }
            }
            remove { _onConnected -= value; }
        }

        public event AsyncEventHandler<MqttClientDisconnectedEventArgs> OnDisconnected
        {
            add { _onDisconnected += value; }
            remove { _onDisconnected -= value; }
        }

        public event AsyncEventHandler<MqttApplicationMessageReceivedEventArgs> OnMessage
        {
            add { _onMessage += value; }
            remove { _onMessage -= value; }
        }

        public async Task<IManagedMqttClient> StartAsync(CancellationToken cancellationToken = default)
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
            _ = Task.Run(async () => await TryConnectAsync(_settings, cancellationToken));
            return this;
        }
        public async Task<IManagedMqttClient> StopAsync(CancellationToken cancellationToken = default)
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


        #endregion
        #region IMqttClientConnectedHandler
        public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            _client.ConnectedHandler = null;
            _client.DisconnectedHandler = this;
            if (_onConnected != null)
            {
                await _onConnected.InvokeAsync(this, eventArgs);
            }
        }
        #endregion
        #region IMqttClientDisconnectedHandler
        public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            _client.DisconnectedHandler = null;
            _client.ConnectedHandler = this;
            if (_onDisconnected != null)
            {
                await _onDisconnected.InvokeAsync(this, eventArgs);
            }
            await TryConnectAsync(_settings, CancellationToken.None);
        }
        #endregion
        #region IMqttApplicationMessageReceivedHandler
        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            await _onMessage?.InvokeAsync(this, eventArgs);
        }
        #endregion
        private async Task TryConnectAsync(ManagedBrokerOptions settings, CancellationToken cancellationToken)
        {
            try
            {
                await _connectingLock.WaitAsync(cancellationToken);

                if (!_client.IsConnected)
                {
                    try
                    {
                        var stoppedToken = _stoppedSource.Token;
                        using (CancellationTokenExtensions.CombinedCancellationToken cts = stoppedToken.CombineWith(cancellationToken))
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
    }
}
