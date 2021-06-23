using System;

namespace BlueForest.Messaging
{
    public class ManagedBrokerOptions
    {
        public BrokerOptions ClientOptions { get; set; }
        public TimeSpan? AutoReconnectMaxDelay { get; set; }
    }
}