using Microsoft.VisualStudio.Threading;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using System.Threading;
using System.Threading.Tasks;

namespace BlueForest.Messaging.MqttNet
{
    public interface IManagedMqttClient 
    {
        event AsyncEventHandler<MqttClientConnectedEventArgs> OnConnected;
        event AsyncEventHandler<MqttClientDisconnectedEventArgs> OnDisconnected;
        event AsyncEventHandler<MqttApplicationMessageReceivedEventArgs> OnMessage;
        Task<IManagedMqttClient> StartAsync(CancellationToken cancellationToken = default);
        Task<IManagedMqttClient> StopAsync(CancellationToken cancellationToken = default);
        IMqttClient Client { get; }
    }
}
