namespace BlueForest.Messaging.JsonRpc
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    public class RpcTopic : IRpcTopic, IEquatable<IRpcTopic>
    {
        int? _hash;
        protected string _p, _s, _c, _n, _f, _t;

        public RpcTopic() { }

        public RpcTopic(IRpcTopic other) : this(other.Path, other.Stream, other.Channel, other.Namespace, other.From, other.To)
        {
        }

        public RpcTopic(string path, string stream, string channel, string @namespace, string from, string to)
        {
            _p = path;
            _s = stream;
            _c = channel;
            _n = @namespace;
            _f = from;
            _t = to;
            ComputeHash();
        }

        public string Path { get => _p; set{ _p = value; _hash = null; } }
        public string Stream { get => _s; set { _s = value; _hash = null; } }
        public string Channel { get => _c; set { _c = value; _hash = null; } }
        public string Namespace { get => _n; set { _n = value; _hash = null; } }
        public string From { get => _f; set { _f = value; _hash = null; } }
        public string To { get => _t; set { _t = value; _hash = null; } }

        public IRpcTopic ReverseInPlace()
        {
            var tmp = _f;
            _f = _t;
            _t = tmp;
            return this;
        }

        public bool Equals([AllowNull] IRpcTopic other)
        {
            if (other == null) return false;
            if( other.GetHashCode() != GetHashCode() ) return false;
            return (other.Path?.Equals(_p) ?? _p == null) &&
                   (other.Stream?.Equals(_s) ?? _s == null) &&
                   (other.Channel?.Equals(_c) ?? _c == null) &&
                   (other.Namespace?.Equals(_n) ?? _n == null) &&
                   (other.From?.Equals(_f) ?? _f == null) &&
                   (other.To?.Equals(_t) ?? _t == null);
        }

        public override string ToString()
        {
            return DefaultTopicLogic.Shared.Assemble(this, TopicUse.Publish);
        }

        public override int GetHashCode()
        {
            _hash = _hash ?? ComputeHash();
            return (int)_hash;
        }

        private int ComputeHash()=> HashCode.Combine(_p?.GetHashCode() ?? 0,
                                     _s?.GetHashCode() ?? 0,
                                     _c?.GetHashCode() ?? 0,
                                     _n?.GetHashCode() ?? 0,
                                     _f?.GetHashCode() ?? 0,
                                     _t?.GetHashCode() ?? 0);
    }
}
