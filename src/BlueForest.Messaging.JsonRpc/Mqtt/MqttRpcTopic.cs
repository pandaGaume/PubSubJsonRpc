namespace BlueForest.Messaging.JsonRpc
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;

    public class MqttRpcTopic : RpcTopicBase
    {
        internal static Regex ValidateExpression = new Regex(@"^((\w+)/)+(\w+)$", RegexOptions.Compiled);

        public const byte SEPARATOR = (byte)'/';

        public static MqttRpcTopic Parse(string str, Encoding encoding = null)
        {
            var e = encoding ?? Encoding.UTF8;
            return Parse(e.GetBytes(str)); 
        }
        public static MqttRpcTopic Parse(ReadOnlyMemory<byte> mem)
        {
            var span = mem.Span;
            var i = span.LastIndexOf(SEPARATOR);
            if( i > 0)
            {
                var to = mem[i..];
                span = span.Slice(0,i);
                var j = span.LastIndexOf(SEPARATOR);
                if( j > 0)
                {
                    var from = mem[j..i];
                    var path = mem.Slice(0, j);
                    return new MqttRpcTopic(path, from, to);
                }
            }
            return default;
        }

        public MqttRpcTopic(ReadOnlyMemory<byte> path, ReadOnlyMemory<byte> from, ReadOnlyMemory<byte> to) : base(path, from, to) 
        { 
        }
    }
}
