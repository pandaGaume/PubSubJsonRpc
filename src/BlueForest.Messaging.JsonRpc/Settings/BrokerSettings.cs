﻿namespace BlueForest.Messaging.JsonRpc
{
    public class BrokerSettings
    {
        public string Host { get; set; }
        public int? Port { get; set; }
        public bool? IsSecure { get; set; }
        public string ClientId { get; set; }
        public BrokerClientCredential Credentials { get; set; }
    }
}
