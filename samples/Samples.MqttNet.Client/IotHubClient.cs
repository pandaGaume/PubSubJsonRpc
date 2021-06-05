using BlueForest.Messaging.JsonRpc;
using BlueForest.Messaging.JsonRpc.MQTTnet;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Samples.Commons;
using Samples.Commons.JsonRpc;
using StreamJsonRpc;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Samples.MqttNet.Client
{
    public class IotHubClientSettings
    {
        public Commons.Mqtt.MqttClientOptions Broker { get; set; }
        public PubSubOptions Publish { get; set; }
        public JsonRpcSettings Rpc { get; set; }
    }

    public class IotHubClient : IDisposable
    {
        IotHubClientSettings _settings;
        IManagedMqttClient _broker;
        JsonRpcPubSubService _rpc;

        public async ValueTask<JsonRpcPubSubService> StartClientAsync(IotHubClientSettings settings)
        {
            // API
            _settings = settings;

            // MQTT
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(_settings.Broker.ClientIdTemplate)
                .WithCredentials(_settings.Broker.UserName, _settings.Broker.Password)
                .WithTcpServer(_settings.Broker.Host, _settings.Broker.Port)
                .WithCleanSession();

            var options = (_settings.Broker.IsSecure ? optionsBuilder.WithTls() : optionsBuilder).Build();
            var managedOptions = new ManagedMqttClientOptionsBuilder().WithAutoReconnectDelay(TimeSpan.FromSeconds(30)).WithClientOptions(options).Build();
            _broker = new MqttFactory().CreateManagedMqttClient();
            await _broker.StartAsync(managedOptions);

            // RPC
            var mqttProxy = new MqttClientNetInterface(_broker.InternalClient);
            var topic = MqttRpcTopic.Parse(_settings.Rpc.Topic);
            _rpc = new JsonRpcPubSubService(mqttProxy, topic, settings.Publish);
            _rpc.Attach(typeof(IIotHub));
            _rpc.StartListening();
            return _rpc;
        }
        public void Dispose()
        {
            _rpc?.Dispose();
            _ = _broker?.StopAsync();
        }
    }
}
