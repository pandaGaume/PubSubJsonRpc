using MQTTnet.Client.Options;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public static class BrokerSettingsExtensions
    {
        public static IMqttClientOptions BuildMqttClientOptions(this BrokerSettings settings)
        {

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(settings.ClientId)
                .WithTcpServer(settings.Host, settings.Port)
                .WithCleanSession();

            optionsBuilder = settings.Credentials != null && settings.Credentials.UserName != null && settings.Credentials.Password != null ? optionsBuilder.WithCredentials(settings.Credentials.UserName, settings.Credentials.Password) : optionsBuilder;
            var options = (settings.IsSecure ?? false ? optionsBuilder.WithTls() : optionsBuilder).Build();
            return options;
        }

        public static BrokerSession GetMainSession(this JsonRpcMqttSettings settings) => settings.Sessions[settings.MainSession ?? 0];
        public static BrokerRoute GetMainRoute(this BrokerSession session) => session.Routes[session.MainRoute ?? 0];

    }
}
