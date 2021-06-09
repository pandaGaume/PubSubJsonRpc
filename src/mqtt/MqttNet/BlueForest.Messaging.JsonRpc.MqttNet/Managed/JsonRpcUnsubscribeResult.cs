using MQTTnet.Client.Unsubscribing;


namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public class JsonRpcUnsubscribeResult
    {
        readonly MqttClientUnsubscribeResultCode _code;

        public JsonRpcUnsubscribeResult(MqttClientUnsubscribeResultCode code)
        {
            _code = code;
        }

        public MqttClientUnsubscribeResultCode Code => _code;
    }
}
