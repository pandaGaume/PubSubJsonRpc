namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcOptionsBuilder
    {
        JsonRpcManagedMqttClient _client = null;
        JsonRpcManagedClientBuilder _builder = null;
        BrokerSession _session = null;
        BrokerRoute _route = null;
        int? _routeIndex = null;

        public MqttJsonRpcOptions Build()=> new MqttJsonRpcOptions()
            {
                MqttClient = _client?? _builder?.Build(),
                Session = _session,
                Route = _route?? _session.Routes[_routeIndex??(_session.MainRoute??0)]
            };

        public MqttJsonRpcOptionsBuilder WithClient(JsonRpcManagedMqttClient client)
        {
            _client = client;
            return this;
        }
        public MqttJsonRpcOptionsBuilder WithClient(JsonRpcManagedClientBuilder builder)
        {
            _builder = builder;
            return this;
        }
        public MqttJsonRpcOptionsBuilder WithSession(BrokerSession session)
        {
            _session = session;
            return this;
        }
        public MqttJsonRpcOptionsBuilder WithRoute(BrokerRoute route)
        {
            _route = route;
            return this;
        }
        public MqttJsonRpcOptionsBuilder WithRouteIndex(int routeIndex)
        {
            _routeIndex = routeIndex;
            return this;
        }
    }
}
