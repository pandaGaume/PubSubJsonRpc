namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcManagedClientBuilder
    {
        ManagedBrokerOptions _options;
        ManagedBrokerOptionsBuilder _builder;

        public JsonRpcManagedClientBuilder WithOptions(ManagedBrokerOptions o)
        {
            _options = o;
            return this;
        }

        public JsonRpcManagedClientBuilder WithOptions(ManagedBrokerOptionsBuilder b)
        {
            _builder = b;
            return this;
        }

        public JsonRpcManagedMqttClient Build() => new JsonRpcManagedMqttClient(_options ?? _builder?.Build());
    }
}
