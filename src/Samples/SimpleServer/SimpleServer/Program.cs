using BlueForest.Messaging;
using BlueForest.Messaging.JsonRpc;
using BlueForest.Messaging.JsonRpc.MqttNet;
using BlueForest.MqttNet;
using Devices;
using System.Threading.Tasks;

namespace SimpleServer
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            // create the service
            var service = new Switch();
            var rpc = new MqttJsonRpcServer<ISwitchApi>(service);

            // low level broker settings 
            var brokerOptions = new BrokerOptions()
            {
                Host = "test.mosquitto.org",
                Port = 1883,
                ClientId = "dotvision-BlueForest.SwitchService_075C86C1A",
                //Credentials = new BrokerClientCredential()
                //{
                //    UserName = "username",
                //    Password = "password"
                //},
                IsSecure = false
            };

            // rpc logic metadata
            var session = new JsonRpcBrokerSession()
            {
                Name = "SwitchService",
                // you can define more than one route, according differents services and logics
                Routes = new JsonRpcBrokerRoute[]
                {
                    new JsonRpcBrokerRoute()
                    {
                        // change here the channel topics values 
                        // default is DefaultTopicLogic.Shared.ChannelNames
                        Channels = new JsonRpcBrokerChannels()
                        {
                            Request = "0",
                            Response = "1",
                            Notification = "2",
                        },
                        // for a server, From & To are unecessary and are equals to Name & Incoming From topic
                        Namespace = "BlueForest",
                        Path = "dotvision",
                        // quality of service used for the route
                        Qos = 1
                    }
                }
            };

            // build the managed client.
            var managedBrokerOptionsBuilder = new ManagedBrokerOptionsBuilder().WithMqttBrokerSettings(brokerOptions);
            var managedClientBuilder = new ManagedMqttClientBuilder().WithOptions(managedBrokerOptionsBuilder);

            // build the options.
            var optionsBuilder = new MqttJsonRpcServiceOptionsBuilder()
                .WithClient(managedClientBuilder)
                .WithSession(session)
                .WithRoute(session.GetMainRoute());

            // start the service with the above options
            await rpc.StartAsync(optionsBuilder.Build());

            await service.Completion;
        }
    }
}
