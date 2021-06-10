using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BlueForest.Messaging.JsonRpc
{
    public class MqttRpcTopic : AbstractRpcTopic
    {
        internal static Regex ValidateExpression = new Regex(@"^((\w+)/)+(\w+)$", RegexOptions.Compiled);

        public const byte SEPARATOR = (byte)'/';
        public const byte SINGLE_LEVEL_WILDCHAR = (byte)'+';
        public const byte MULTI_LEVEL_WILDCHAR = (byte)'#';
        
        public const string SEPARATOR_STR = "/";
        public const string SINGLE_LEVEL_WILDCHAR_STR = "+";
        public const string MULTI_LEVEL_WILDCHAR_STR = "#";
        public static readonly string NONE_STR = String.Empty;

        public static MqttRpcTopic Parse(string str, Encoding encoding = null)
        {
            var parts = str.Split('/');
            var l = parts.Length;
            
            MqttRpcTopic t = new MqttRpcTopic();
            var e = encoding ?? Encoding.UTF8;
            if (l > 1) t.Path = e.GetBytes(string.Join('/',parts[0],parts[1])); // <path>/<steam>
            if (l > 2) t.Channel = e.GetBytes(parts[2]);
            if (l > 3) t.Namespace = e.GetBytes(parts[3]);
            if (l > 4) t.From = e.GetBytes(parts[4]);
            if (l > 5) t.To = e.GetBytes(parts[5]);

            return t;
        }



        public MqttRpcTopic() : base()
        {

        }
        public MqttRpcTopic(IRpcTopic other) : base(other)
        {

        }
        public MqttRpcTopic( string path, string channel, string @namespace, string from, string to, Encoding encoding = null) : this((encoding ?? Encoding.UTF8).GetBytes(path), (encoding ?? Encoding.UTF8).GetBytes(channel), (encoding ?? Encoding.UTF8).GetBytes(@namespace), (encoding ?? Encoding.UTF8).GetBytes(from ?? string.Empty), (encoding ?? Encoding.UTF8).GetBytes(to ?? string.Empty))
        {
        }

        public MqttRpcTopic(ReadOnlyMemory<byte> path, ReadOnlyMemory<byte> channel, ReadOnlyMemory<byte> @namespace, ReadOnlyMemory<byte> from, ReadOnlyMemory<byte> to) : base(path, channel, @namespace, from, to)
        {
        }
    }
}
