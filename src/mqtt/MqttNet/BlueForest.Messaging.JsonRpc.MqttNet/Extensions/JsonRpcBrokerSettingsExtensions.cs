namespace BlueForest.Messaging.JsonRpc
{
    public static class JsonRpcBrokerSettingsExtensions
    {
        public static JsonRpcBrokerSession GetMainSession(this JsonRpcMqttOptions settings) => settings.Sessions[settings.MainSession ?? 0];
        public static JsonRpcBrokerRoute GetMainRoute(this JsonRpcBrokerSession session) => session.Routes[session.MainRoute ?? 0];

    }
}
