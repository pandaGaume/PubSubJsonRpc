using System;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class ManagedBrokerOptionsBuilder
    {
        BrokerSettings _settings;
        TimeSpan? _autoReconnectDelay;

        public ManagedBrokerOptionsBuilder WithMqttBrokerSettings(BrokerSettings s)
        {
            _settings = s;
            return this;
        }

        public ManagedBrokerOptionsBuilder WithAutoReconnectMaxDelay(TimeSpan timeSpan)
        {
            _autoReconnectDelay = timeSpan;
            return this;
        }

        public ManagedBrokerOptions Build()=>new ManagedBrokerOptions() { ClientOptions = _settings, AutoReconnectMaxDelay = _autoReconnectDelay };
    }
}
