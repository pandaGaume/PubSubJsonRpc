using BlueForest.Geofencing.Api;
using BlueForest.Messaging.JsonRpc;
using BlueForest.Messaging.JsonRpc.MQTTnet;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Samples.MqttNet.Client
{
    class Program
    {
        public static string ConfigPathKey = "config";
        public static string DefaultConfigPath = "clientsettings.json";

        public const string Signature =
         "    ____  _              ______                  _\r\n"
        + "   |  _ \\| |            |  ____|                | |\r\n"
        + "   | |_) | |_   _  ___  | |__ ___  _ __ ___  ___| |_ \r\n"
        + "   |   _<| | | | |/ _ \\ |  __/ _ \\| '__/ _ \\/ __| __|\r\n"
        + "   | |_) | | |_| |  __/ | | | (_) | | | __/\\__ \\ |_ \r\n"
        + "   |____/|_|\\__,_|\\___| |_|  \\___/|_|  \\___||___/\\__|\r\n"
        + "\r\n"
        + "   {0}\r\n"
        + "   (c) DotVision 2021\r\n";

        static async Task Main(string[] args)
        {
            Console.WriteLine(Signature, "Rpc Json over Mqtt - MqttNet Client sample - v1.0");

            var switchMappings = new Dictionary<string, string>()
            {
                { "-c", ConfigPathKey }
            };

            var commandLineConfig = new ConfigurationBuilder().AddCommandLine(args, switchMappings).Build();

            string configPath = commandLineConfig[ConfigPathKey] ?? DefaultConfigPath;

            using (var client = await StartClientAsync(await GetJsonConfigAsync(configPath)))
            {
            }
        }

        static async Task<IConfigurationRoot> GetJsonConfigAsync(string path)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
            {
                using HttpClient client = new HttpClient();
                return new ConfigurationBuilder().AddJsonStream(await LoadJsonStreamAsync(client, uri)).Build();
            }

            return new ConfigurationBuilder().AddJsonFile(path).Build();
        }

        static async Task<Stream> LoadJsonStreamAsync(HttpClient client, Uri url)
        {
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStreamAsync();
                }
            }
            return null;
        }

        public static async Task<JsonRpcPubSubService> StartClientAsync(IConfigurationRoot config)
        {
            // note : this is an extension located in Microsoft.Extensions.Configuration.Binder 
            var settings = config.Get<IotHubClientSettings>();
            // MQTT
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(settings.Broker.ClientIdTemplate)
                .WithCredentials(settings.Broker.UserName, settings.Broker.Password)
                .WithTcpServer(settings.Broker.Host, settings.Broker.Port)
                .WithCleanSession();

            var options = (settings.Broker.IsSecure ? optionsBuilder.WithTls() : optionsBuilder).Build();
            var managedOptions = new ManagedMqttClientOptionsBuilder().WithAutoReconnectDelay(TimeSpan.FromSeconds(30)).WithClientOptions(options).Build();
            var broker = new MqttFactory().CreateManagedMqttClient();
            await broker.StartAsync(managedOptions);

            // RPC
            var mqttProxy = new MqttClientNetInterface(broker.InternalClient);
            var topic = MqttRpcTopic.Parse(settings.Rpc.Topic);
            var rpc = new JsonRpcPubSubService(mqttProxy, topic, settings.Publish);
            rpc.Attach(typeof(IGeofencingAPI));
            rpc.StartListening();
            return rpc;
        }
    }
}

