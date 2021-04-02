using MQTTnet.Client;
using StreamJsonRpc;
using System;
using System.Text;
using System.Threading.Tasks;

namespace BlueForest.Messaging.JsonRpc.MQTTnet
{
    public class JsonRpcBuilder
    {
        IMqttClient _client;
        Encoding _encoding;
        MqttRpcTopic _topic;
        Object _api;

        public JsonRpcBuilder WithMqttClient(IMqttClient client)
        {
            _client = client;
            return this;
        }
        public JsonRpcBuilder WithEncoding(Encoding encoding)
        {
            _encoding = encoding;
            return this;
        }
        public JsonRpcBuilder WithTopic(MqttRpcTopic topic)
        {
            _topic = topic;
            return this;
        }
        public JsonRpcBuilder WithObject(Object api)
        {
            _api = api;
            return this;
        }

        public StreamJsonRpc.JsonRpc Build()
        {
            var mqttProxy = new MqttClientJsonRpcInterface(_client);
            var formatter = new JsonMessageFormatter(_encoding??Encoding.UTF8);
            var handler = new PubSubJsonRpcMessageHandler(mqttProxy, _topic, formatter);
            return new StreamJsonRpc.JsonRpc(handler, _api);
        }
    }
}
