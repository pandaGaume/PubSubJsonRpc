using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Samples.MqttNet.Service
{
    public class Program
    {
        public static string ConfigPathKey = "config";
        public static string DefaultConfigPath = "servicesettings.json";

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
            Console.WriteLine(Signature, "Rpc Json over Mqtt - MqttNet service sample - v1.0");

            var switchMappings = new Dictionary<string, string>()
            {
                { "-c", ConfigPathKey }
            };

            var commandLineConfig = new ConfigurationBuilder().AddCommandLine(args, switchMappings).Build();

            string configPath = commandLineConfig[ConfigPathKey] ?? DefaultConfigPath;

            await StartServiceAsync(await GetJsonConfigAsync(configPath));

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

        public static async Task StartServiceAsync(IConfigurationRoot config)
        {
            // note : this is an extension located in Microsoft.Extensions.Configuration.Binder 
            var settings = config.Get<IotHubSettings>();
            using var hub = new IotHubService();
            await hub.StartServiceAsync(settings);
            while (true) { await Task.Delay(10000); }
        }
    }
}
