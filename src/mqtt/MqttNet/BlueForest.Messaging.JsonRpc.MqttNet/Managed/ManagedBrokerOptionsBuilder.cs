using System;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class ManagedBrokerOptionsBuilder
    {
        BrokerOptions _settings;
        TimeSpan? _autoReconnectDelay;

        public ManagedBrokerOptionsBuilder WithMqttBrokerSettings(BrokerOptions s)
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
