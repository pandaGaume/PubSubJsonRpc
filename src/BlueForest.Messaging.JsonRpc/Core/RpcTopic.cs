namespace BlueForest.Messaging.JsonRpc
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    public class RpcTopic : IRpcTopic, IEquatable<IRpcTopic>
    {
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
        }

        public string Path { get => _p; set => _p = value; }
        public string Stream { get => _s; set => _s = value; }
        public string Channel { get => _c; set => _c = value; }
        public string Namespace { get => _n; set => _n = value; }
        public string From { get => _f; set => _f = value; }
        public string To { get => _t; set => _t = value; }

        public IRpcTopic ReverseInPlace()
        {
            var tmp = _f;
            _f = _t;
            _t = tmp;
            return this;
        }

        public bool Equals([AllowNull] IRpcTopic other)
        {
            return (other?.Path.Equals(_p) ?? false) &&
                   (other?.Stream.Equals(_s) ?? false) &&
                   (other?.Channel.Equals(_c) ?? false) &&
                   (other?.Namespace.Equals(_n) ?? false) &&
                   (other?.From.Equals(_f) ?? false) &&
                   (other?.To.Equals(_t) ?? false);
        }

        public override string ToString()
        {
            return DefaultTopicLogic.Shared.Assemble(this, TopicUse.Publish);
        }
    }
}
