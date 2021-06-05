using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public static class BrokerSettingsExtensions
    {
        public static async ValueTask<IManagedMqttClient> GetMqttClientAsync(this BrokerSettings[] settings, int index, IManagedMqttClient[] clients)
        {
            if (index < clients.Length && index >= 0)
            {
                if (clients[index] == null)
                {
                    var s = settings[index];
                    var optionsBuilder = new MqttClientOptionsBuilder()
                        .WithClientId(s.ClientId)
                        .WithCredentials(s.Credentials.UserName, s.Credentials.Password)
                        .WithTcpServer(s.Host, s.Port)
                        .WithCleanSession();

                    var options = (s.IsSecure ?? false ? optionsBuilder.WithTls() : optionsBuilder).Build();

                    var c = new MqttFactory().CreateManagedMqttClient();
                    var managedOptions = new ManagedMqttClientOptionsBuilder().WithAutoReconnectDelay(TimeSpan.FromSeconds(30)).WithClientOptions(options).Build();
                    await c?.StartAsync(managedOptions);
                    clients[index] = c;
                }
                return clients[index];
            }
            return default;
        }

    }
}
