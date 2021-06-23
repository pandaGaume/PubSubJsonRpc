using BlueForest.Messaging.MqttNet;

namespace BlueForest.Messaging.MqttNet
{
    public class ManagedMqttClientBuilder
    {
        ManagedBrokerOptions _options;
        ManagedBrokerOptionsBuilder _builder;

        public ManagedMqttClientBuilder WithOptions(ManagedBrokerOptions o)
        {
            _options = o;
            return this;
        }

        public ManagedMqttClientBuilder WithOptions(ManagedBrokerOptionsBuilder b)
        {
            _builder = b;
            return this;
        }

        public IManagedMqttClient Build() => new ManagedMqttClient(_options ?? _builder?.Build());
    }
}
