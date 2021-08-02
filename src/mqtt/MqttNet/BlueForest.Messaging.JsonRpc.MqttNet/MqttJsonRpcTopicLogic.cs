using BlueForest.MqttNet;
using System;
using System.Text;

namespace BlueForest.Messaging.JsonRpc
{
    public class MqttJsonRpcTopicLogic : IRpcTopicLogic
    {
        const string StreamDefault = "rpc";
        const int MinimumPartsCount = 5;

        static readonly JsonRpcBrokerChannels ChannelsDefault = new JsonRpcBrokerChannels() { Request = "0", Response = "1", Notification = "2" };
        public static readonly IRpcTopicLogic Shared = new MqttJsonRpcTopicLogic()
        {
            StreamName = StreamDefault,
            ChannelNames = ChannelsDefault
        };

        public const char SEPARATOR = '/';
        public const string SINGLE_LEVEL_WILD_STR = "+";
        public const string MULTI_LEVEL_WILD_STR = "#";

        string _stream = StreamDefault;
        JsonRpcBrokerChannels _channels = ChannelsDefault;

        public string StreamName { get => _stream; set => _stream = value; }
        public JsonRpcBrokerChannels ChannelNames { get => _channels; set => _channels = value; }

        public string Assemble(IRpcTopic topic, TopicUse usage)
        {
            string[] tmp = ToArray(topic);
            StringBuilder sb = new StringBuilder();
            int i = 0;

            if (usage == TopicUse.Subscribe)
            {
                i = tmp.Length;
                while (i >= 0 && tmp[--i] == null) ;
                if (i != tmp.Length - 1)
                {
                    tmp[i] = MULTI_LEVEL_WILD_STR;
                }
                for (; i >= 0; i--)
                {
                    if (tmp[i] == null) tmp[i] = SINGLE_LEVEL_WILD_STR;
                }
                i = 0;
            }
            sb.Append(tmp[i++]);
            while (i < tmp.Length && tmp[i] != null)
            {
                sb.Append(SEPARATOR).Append(tmp[i++]);
            }

            return sb.ToString();
        }

        public bool Match(IRpcTopic a, IRpcTopic b) => ToArray(a).Match(ToArray(b));

        public IRpcTopic Parse(string topicStr)
        {
            string[] tmp = topicStr.Split(SEPARATOR);
            if (tmp.Length < MinimumPartsCount)
            {
                throw new FormatException();
            }
            var s = _stream ?? StreamDefault;
            int i = 0;
            for (; i != tmp.Length; i++)
            {
                if (string.Compare(tmp[i], s) == 0) break;
            }
            if (i > (tmp.Length - MinimumPartsCount + 1))
            {
                throw new FormatException();
            }
            var t = new MqttJsonRpcTopic();
            t.Path = String.Join(SEPARATOR, tmp, 0, i);
            t.Stream = tmp[i++];
            t.Channel = tmp[i++];
            t.Namespace = tmp[i++];
            t.From = tmp[i++];
            if (i < tmp.Length)
            {
                t.To = tmp[i];
            }
            return t;
        }

        public string[] ToArray(IRpcTopic topic)
        {
            return new string[]{ topic.Path, topic.Stream, topic.Channel, topic.Namespace, topic.From, topic.To };
        }
    }
}
