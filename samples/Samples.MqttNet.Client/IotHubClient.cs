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
        public JsonRpcSettings Rpc { get; set; }
    }

    public class IotHubClient : IDisposable
    {
        IotHubClientSettings _settings;
        IManagedMqttClient _broker;
        JsonRpc _rpc;
        IIotHub _proxy;

        public async ValueTask StartClientAsync(IotHubClientSettings settings)
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

            // start the broker
            await _broker.StartAsync(managedOptions);

            // RPC
            var mqttProxy = new MqttClientNetInterface(_broker.InternalClient);
            var formatter = new JsonMessageFormatter(Encoding.UTF8);
            var topic = MqttRpcTopic.Parse("dotvision/rpc/0/IotHub-Client001/IotHub-001");
            var handler = new PubSubJsonRpcMessageHandler(mqttProxy, topic, formatter);
            _rpc  = new StreamJsonRpc.JsonRpc(handler);
            _proxy = (IIotHub) _rpc.Attach(typeof(IIotHub));
            _rpc.StartListening();
        }
        public IIotHub Proxy => _proxy;
        public void Dispose()
        {
        }
    }
}
