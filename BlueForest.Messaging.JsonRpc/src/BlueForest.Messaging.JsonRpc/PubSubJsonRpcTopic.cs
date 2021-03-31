namespace BlueForest.Messaging.JsonRpc
{
    using System;
    using System.Buffers;
    using System.Text;

    /// <summary>
    /// An utility class to hold topic parts
    /// </summary>
    internal class TopicSegment : ReadOnlySequenceSegment<byte>
    {
        public TopicSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public TopicSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new TopicSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;
            return segment;
        }
    }

    /// <summary>
    /// Class to support topic syntax options.
    /// </summary>
    public class TopicSyntaxOptions
    {
        public static readonly TopicSyntaxOptions Default = new TopicSyntaxOptions();

        public const string MULTI_LEVEL_WILDCARD_STR_DEFAULT = "#";
        public const string SINGLE_LEVEL_WILDCARD_STR_DEFAULT = "*";
        public const byte SEP_BYTE_DEFAULT = (byte)'/';

        public string MultiLevelWildcard = MULTI_LEVEL_WILDCARD_STR_DEFAULT;
        public string SingleLevelWildcard = SINGLE_LEVEL_WILDCARD_STR_DEFAULT;
        public byte Separator = SEP_BYTE_DEFAULT;
    }

    /// <summary>
    /// A class to handle Pub-Sub Destination logic.
    /// The Topic logic is base on the address pattern {path}/{from}/{to} where 
    /// <list type="bullet">
    /// <item><description>path is the base address of the Mqtt topic and can be anything.</description></item>
    /// <item><description>From is the source of the message and must be unique.</description></item>
    /// <item><description>To is the destination of the message and must be unique.</description></item>
    /// </list>
    /// Special case is for Many to One scenario when the pattern become {path}/#/{to}.
    /// Broadcast is also supported using {path}/{from}/* but MUST be use with caution.
    /// </summary>
    public class PubSubJsonRpcTopic 
    {

        private static byte[] GetBytes(string str, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetBytes(str ?? string.Empty);
        }

        public static implicit operator ReadOnlySequence<byte>(PubSubJsonRpcTopic t) => t.AsRequest();

        public static PubSubJsonRpcTopic Generic(string path, string to, Encoding encoding = null, TopicSyntaxOptions options = null )
        {
            var o = options ?? TopicSyntaxOptions.Default;
            return new PubSubJsonRpcTopic(path, o.MultiLevelWildcard, to, encoding);
        }

        public static PubSubJsonRpcTopic Broadcast(string path, string from, Encoding encoding = null, TopicSyntaxOptions options = null)
        {
            var o = options ?? TopicSyntaxOptions.Default;
            return new PubSubJsonRpcTopic(path, from, o.SingleLevelWildcard, encoding);
        }

        /// <summary>
        /// Try to parse a sequence of byte to a <cref>MqttJsonRpcTopic</cref>.
        /// </summary>
        /// <param name="str">the sequence to parse.</param>
        /// <param name="topic">the parsed topic, null if not able to parse.</param>
        /// <returns>true for a successfull parse, false otherwise.</returns>
        public static bool TryParse(ReadOnlySequence<byte> str, out PubSubJsonRpcTopic topic, TopicSyntaxOptions options = null)
        {
            var o = options ?? TopicSyntaxOptions.Default;
            ReadOnlyMemory<byte> memory = str.ToArray(); // we make a copy to ensure ownership
            var span = memory.Span;
            var i = span.LastIndexOf(o.Separator);
            if( i >= 0)
            {
                var to = memory[i..];
                span = span.Slice(0, i);
                i = span.LastIndexOf(o.Separator);
                if( i >= 0)
                {
                    var from = memory[i..];
                    var path = memory.Slice(0, i);
                    topic = new PubSubJsonRpcTopic(path, from, to);
                    return true;
                }
            }
            topic = null;
            return false;
        }

        readonly ReadOnlyMemory<byte> _path;
        readonly ReadOnlyMemory<byte> _from;
        readonly ReadOnlyMemory<byte> _to;

        public PubSubJsonRpcTopic(string path, string from, string to, Encoding encoding = null) :
            this(GetBytes(path, encoding), GetBytes(from, encoding), GetBytes(to, encoding))
        {
        }

        public PubSubJsonRpcTopic(ReadOnlyMemory<byte> path, ReadOnlyMemory<byte> from, ReadOnlyMemory<byte> to)
        {
            _path = path;
            _from = from;
            _to = to;
        }

        /// <summary>
        /// Get the topic as {path}/{from}/{to} pattern.
        /// </summary>
        /// <returns>the sequence representing the pattern.</returns>
        public ReadOnlySequence<byte> AsRequest()
        {
            var first = new TopicSegment(_path);
            var last = first.Append(_from).Append(_to);
            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        /// <summary>
        /// Get the topic as {path}/{to}/{from} pattern.
        /// </summary>
        /// <returns>the sequence representing the pattern.</returns>
        public ReadOnlySequence<byte> AsResponse()
        {
            var first = new TopicSegment(_path);
            var last = first.Append(_to).Append(_from);
            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }
    }
}
