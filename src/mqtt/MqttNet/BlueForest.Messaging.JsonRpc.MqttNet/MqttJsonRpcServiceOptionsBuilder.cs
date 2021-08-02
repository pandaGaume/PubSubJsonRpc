using BlueForest.MqttNet;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcServiceOptionsBuilder
    {
        IManagedMqttClient _client = null;
        ManagedMqttClientBuilder _builder = null;
        JsonRpcBrokerSession _session = null;
        JsonRpcBrokerRoute _route = null;
        int? _routeIndex = null;


        public MqttJsonRpcServiceOptions Build() => new MqttJsonRpcServiceOptions()
        {
            MqttClient = _client ?? _builder?.Build(),
            Session = _session,
            Route = _route ?? _session.Routes[_routeIndex ?? (_session.MainRoute ?? 0)]
        };

        public MqttJsonRpcServiceOptionsBuilder WithClient(IManagedMqttClient client)
        {
            _client = client;
            return this;
        }
        public MqttJsonRpcServiceOptionsBuilder WithClient(ManagedMqttClientBuilder builder)
        {
            _builder = builder;
            return this;
        }
        public MqttJsonRpcServiceOptionsBuilder WithSession(JsonRpcBrokerSession session)
        {
            _session = session;
            return this;
        }
        public MqttJsonRpcServiceOptionsBuilder WithRoute(JsonRpcBrokerRoute route)
        {
            _route = route;
            return this;
        }
        public MqttJsonRpcServiceOptionsBuilder WithRouteIndex(int routeIndex)
        {
            _routeIndex = routeIndex;
            return this;
        }
    }
}
