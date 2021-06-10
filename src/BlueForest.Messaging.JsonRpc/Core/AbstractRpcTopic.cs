
namespace BlueForest.Messaging.JsonRpc
{
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    public abstract class AbstractRpcTopic : IRpcTopic, IEquatable<IRpcTopic>
    {
        protected ReadOnlyMemory<byte> _p, _c, _n, _f, _t;

        public AbstractRpcTopic() { }
        public AbstractRpcTopic(IRpcTopic other) : this( other.Path, other.Channel, other.Namespace, other.From, other.To)
        {
        }

        public AbstractRpcTopic(ReadOnlyMemory<byte> path, ReadOnlyMemory<byte> channel, ReadOnlyMemory<byte> @namespace, ReadOnlyMemory<byte> from, ReadOnlyMemory<byte> to)
        {
            _p = path;
            _c = channel;
            _n = @namespace;
            _f = from;
            _t = to;
        }

        public ReadOnlyMemory<byte> Channel { get => _c; set => _c = value; }
        public ReadOnlyMemory<byte> Namespace { get => _n; set => _n = value; }
        public ReadOnlyMemory<byte> Path { get => _p; set => _p = value; }
        public ReadOnlyMemory<byte> From { get => _f; set => _f = value;  }
        public ReadOnlyMemory<byte> To { get => _t; set => _t = value; }

        public IRpcTopic ReverseInPlace()
        {
            var tmp = _f;
            _f = _t;
            _t = tmp;
            return this;
        }

        public bool Equals([AllowNull] IRpcTopic other)
        {
            return (other?.Channel.Equals(_c) ?? false) && (other?.From.Equals(_f) ?? false) && (other?.To.Equals(_t) ?? false) && (other?.Path.Equals(_p) ?? false);
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(this.Assemble().ToArray());
        }
    }
}
