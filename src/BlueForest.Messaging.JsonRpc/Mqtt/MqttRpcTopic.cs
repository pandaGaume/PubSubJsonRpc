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
                var to = mem[(i+1)..];
                span = span.Slice(0,i);
                var j = span.LastIndexOf(SEPARATOR);
                if( j > 0)
                {
                    var from = mem[(j+1)..i];
                    var path = mem.Slice(0, j);
                    return new MqttRpcTopic(path, from, to);
                }
            }
            return default;
        }

        public MqttRpcTopic(string path, string from, string to, Encoding encoding = null) : base( (encoding??Encoding.UTF8).GetBytes(path), (encoding ?? Encoding.UTF8).GetBytes(from), (encoding ?? Encoding.UTF8).GetBytes(to))
        {
        }

        public MqttRpcTopic(ReadOnlyMemory<byte> path, ReadOnlyMemory<byte> from, ReadOnlyMemory<byte> to) : base(path, from, to)
        {
        }
    }
}
