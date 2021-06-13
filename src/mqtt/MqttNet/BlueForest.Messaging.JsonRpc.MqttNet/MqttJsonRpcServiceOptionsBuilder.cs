using System;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class MqttJsonRpcServiceOptionsBuilder
    {
        JsonRpcManagedMqttClient _client = null;
        JsonRpcManagedClientBuilder _builder = null;
        JsonRpcBrokerSession _session = null;
        JsonRpcBrokerRoute _route = null;
        int? _routeIndex = null;
        TimeSpan? _requestTimeout = null;


        public MqttJsonRpcServiceOptions Build()=> new MqttJsonRpcServiceOptions()
            {
                MqttClient = _client?? _builder?.Build(),
                Session = _session,
                Route = _route?? _session.Routes[_routeIndex??(_session.MainRoute??0)],
                RequestTimeout = _requestTimeout
            };

        public MqttJsonRpcServiceOptionsBuilder WithClient(JsonRpcManagedMqttClient client)
        {
            _client = client;
            return this;
        }
        public MqttJsonRpcServiceOptionsBuilder WithClient(JsonRpcManagedClientBuilder builder)
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
        public MqttJsonRpcServiceOptionsBuilder WithRequestTimeout(TimeSpan timeout)
        {
            _requestTimeout = timeout;
            return this;
        }
    }
}
