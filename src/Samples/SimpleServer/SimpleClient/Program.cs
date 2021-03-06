using BlueForest.Messaging.JsonRpc;
using BlueForest.Messaging.JsonRpc.MqttNet;
using BlueForest.MqttNet;
using Devices;
using Microsoft.VisualStudio.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleClient
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            // create the service
            var rpc = new MqttJsonRpcClient<ISwitchApi>();

            // low level broker settings 
            var brokerOptions = new BrokerOptions()
            {
                Host = "test.mosquitto.org",
                Port = 1883,
                ClientId = "dotvision-BlueForest.SwitchService_Controller_075C86C1A",
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
                Name = "SwitchServiceController",
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
                        // for a client, From is unecessary and is equals to Name
                        Namespace = "BlueForest",
                        Path = "dotvision",
                        To = "SwitchService",
                        // quality of service used for the route
                        Qos = 1
                    }
                },
                MainRoute = 0
            };

            // build the managed client.
            // NOTE : we may share one client with several RpcClient
            var managedBrokerOptionsBuilder = new ManagedBrokerOptionsBuilder().WithMqttBrokerSettings(brokerOptions);
            var managedClientBuilder = new ManagedMqttClientBuilder().WithOptions(managedBrokerOptionsBuilder);

            // build the Rpc options.
            var optionsBuilder = new MqttJsonRpcServiceOptionsBuilder()
                .WithClient(managedClientBuilder)
                .WithSession(session)
                .WithRoute(session.GetMainRoute()); // Main route is defined in session, 0 by default

            // start the service with the above options
            await rpc.StartAsync(optionsBuilder.Build());

            // NOTE - we need to wait for the rpc started to gain acces to the underlying service proxy.
            // otherwise its null. 
            var service = rpc.Delegate;

            var cancelToogleDaemon = new CancellationTokenSource();

            Func<Task> toogle = async () =>
            {
                var endOfDeamonToken = cancelToogleDaemon.Token;
                if (!endOfDeamonToken.IsCancellationRequested)
                {
                    var r = new Random();
                    do
                    {
                        try
                        {
                            // set request timeout
                            var src = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            using (CancellationTokenExtensions.CombinedCancellationToken cts = endOfDeamonToken.CombineWith(src.Token))
                            {
                                // call the service
                                await service.ToogleAsync(cts.Token);
                            }
                        }
                        catch (OperationCanceledException) when (endOfDeamonToken.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (OperationCanceledException) when (!endOfDeamonToken.IsCancellationRequested)
                        {
                            // timeout
                        }
                        catch
                        {
                            // internal error
                        }

                        // then wait random time 
                        var d = r.NextDouble() * 10000;
                        await Task.Delay((int)d, endOfDeamonToken);

                    } while (!endOfDeamonToken.IsCancellationRequested);
                }
            };

            Task toogleDeamon = null;
            rpc.Broker.OnConnected += (o, a) =>
            {
                // start the deamon when connected
                toogleDeamon = Task.Run(toogle);
                return Task.CompletedTask;
            };

            rpc.Broker.OnDisconnected += (o, a) =>
            {
                // stop the deamon when disconnected
                cancelToogleDaemon.Cancel();
                return Task.CompletedTask;
            };

            // wait for ever...
            var completion = new TaskCompletionSource<bool>();
            await completion.Task;
        }
    }
}
