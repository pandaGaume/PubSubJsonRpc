using BlueForest.Messaging.JsonRpc;
using BlueForest.Messaging.JsonRpc.MQTTnet;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Samples.Commons;
using Samples.Commons.JsonRpc;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Samples.MqttNet.Service
{
    public class IotHubSettings
    {
        public Commons.Mqtt.MqttClientOptions Broker { get; set; }
        public PubSubOptions Publish { get; set; }
        public JsonRpcSettings Rpc { get; set; }
    }
    public class IotHubService : IDisposable
    {
        IotHubSettings _settings;
        IManagedMqttClient _broker;
        IotHub _hub;
        JsonRpcPubSubService _rpc;

        public async ValueTask<JsonRpcPubSubService> StartServiceAsync(IotHubSettings settings)
        {
            // API
            _settings = settings;
            _hub = new IotHub(ExistingNodes());
            
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
            _rpc = new JsonRpcPubSubService(mqttProxy,topic,settings.Publish);
            _rpc.AddLocalRpcTarget(_hub);
            _rpc.StartListening();
            return _rpc;
        }

        public void Dispose()
        {
            _rpc.Dispose();
            _ = _broker?.StopAsync();
        }

        private IEnumerable<IotNode> ExistingNodes()
        {
            yield return new IotNode("7CA7D3FF-839C-44C6-86E2-9C1AD2229776", "Tracker 12345", "Gps tracker");
            yield return new IotNode("7CA7D3FF-839C-44C6-86E2-9C1AD2229777", "Meter 357", "Energy meter");
            yield return new IotNode("7CA7D3FF-839C-44C6-86E2-9C1AD2229778", "Co2 008", "Co2 sensor");
        }
    }
}
