namespace BlueForest.Messaging.JsonRpc
{
    using Microsoft;
    using StreamJsonRpc;
    using StreamJsonRpc.Protocol;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public class PubSubJsonRpcMessageHandler : MessageHandlerBase, IObserver<IApplicationMessage>, IRequestIdFactory
    {
        public static long IdFactorySeedDefault = 0;
        readonly IRpcTopic _topic;
        readonly IPubSubJsonRpcInterface _client;
        readonly PubSubOptions _options;

        IDisposable _clientSubscription;
        Channel<IApplicationMessage> _inputChannel;

        long _id = IdFactorySeedDefault;
        readonly long _seed = IdFactorySeedDefault;
        IRequestIdFactory _requestIdFactory;
        readonly ConcurrentDictionary<RequestId, (RequestId, IRpcTopic)> _requestIdIndex = new ConcurrentDictionary<RequestId, (RequestId, IRpcTopic)>();

        public PubSubJsonRpcMessageHandler(IPubSubJsonRpcInterface client, IRpcTopic topic, IJsonRpcMessageFormatter formatter, PubSubOptions options = null)
            : base(formatter)
        {
            Requires.NotNull(client, nameof(client));
            Requires.NotNull(topic, nameof(topic));
            Requires.NotNull(formatter, nameof(formatter));
            _client = client;
            _topic = topic;
            _options = options;
         }

        public async ValueTask<PubSubJsonRpcMessageHandler> StartAsync()
        {
            _clientSubscription = _client.Subscribe(this); // IObservable<>
            _inputChannel = Channel.CreateUnbounded<IApplicationMessage>();
            await _client.SubscribeAsync(_topic, _options?.Subscribe);
            return this;
        }
        public async ValueTask<PubSubJsonRpcMessageHandler> StopAsync()
        {
            try
            {
                await _client.UnsubscribeAsync(_topic);
            }
            finally
            {
                _clientSubscription?.Dispose();
                _inputChannel.Writer.TryComplete();
            }
            return this;
        }

        public IRequestIdFactory RequestIdFactory
        {
            get => _requestIdFactory;
            set => _requestIdFactory = value;
        }
        #region  IRequestIdFactory
        public RequestId NextRequestId()
        {
            long v = _id;
            _id = _id == long.MaxValue ? _seed : _id + 1;
            return new RequestId(v);
        }
        #endregion

        #region IObserver<IMqttJsonRpcPublishEvent>
        public virtual void OnCompleted()
        {
        }

        public virtual void OnError(Exception error)
        {
        }

        public virtual void OnNext(IApplicationMessage value)
        {
            _inputChannel.Writer.TryWrite(value);
        }
        #endregion

        #region MessageHandlerBase
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override async Task DisposeAsync()
        {
            try
            {
                await StopAsync();            
            }
            finally
            {
                await base.DisposeAsync();
            }
        }

        protected override ValueTask FlushAsync(CancellationToken cancellationToken) => default;

        protected  override ValueTask<JsonRpcMessage> ReadCoreAsync(CancellationToken cancellationToken) => ReadCoreAsync(_inputChannel, cancellationToken);

        protected  override ValueTask WriteCoreAsync(JsonRpcMessage content, CancellationToken cancellationToken) => WriteCoreAsync(_client, content, cancellationToken);
        #endregion

        protected async virtual ValueTask<JsonRpcMessage> ReadCoreAsync(Channel<IApplicationMessage> input, CancellationToken cancellationToken)
        {
            if (await input.Reader.WaitToReadAsync(cancellationToken))
            {
                if (input.Reader.TryRead(out IApplicationMessage e))
                {
                    var mess = e.Payload.Length > 0 ? this.Formatter.Deserialize(e.Payload) : null;
                    if (mess is JsonRpcRequest request)
                    {
                        var oldId = request.RequestId;
                        var newId = (RequestIdFactory ?? this).NextRequestId();
                        _requestIdIndex.TryAdd(newId, (oldId, e.Topic));
                    }
                    return mess;
                }
            }
            return default;
        }

        protected async virtual ValueTask WriteCoreAsync(IPubSubJsonRpcInterface client, JsonRpcMessage content, CancellationToken cancellationToken)
        {
            var w = new ArrayBufferWriter<byte>();
            this.Formatter.Serialize(w, content);

            var t = _topic;

            if (content is JsonRpcResult result)
            {
                var id = result.RequestId;
                if (_requestIdIndex.TryGetValue(id, out var cache))
                {
                    result.RequestId = cache.Item1;
                    t = cache.Item2.Reverse();
                }
            }
            else if (content is JsonRpcError error)
            {
                var id = error.RequestId;
                if (_requestIdIndex.TryGetValue(id, out var cache))
                {
                    error.RequestId = cache.Item1;
                    t = cache.Item2.Reverse();
                }
            }
            await client.PublishAsync(t, new ReadOnlySequence<byte>(w.GetMemory()), _options?.Publish, cancellationToken);
        }
    }
}
