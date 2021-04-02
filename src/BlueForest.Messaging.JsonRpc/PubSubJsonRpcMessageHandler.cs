namespace BlueForest.Messaging.JsonRpc
{
    using Microsoft;
    using Microsoft.VisualStudio.Threading;
    using StreamJsonRpc;
    using StreamJsonRpc.Protocol;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public class PubSubJsonRpcMessageHandler : MessageHandlerBase, IObserver<IPubSubEvent>, IRequestIdFactory
    {
        public static long IdFactorySeedDefault = 0;
        readonly IRpcTopic _topic;
        readonly IPubSubJsonRpcInterface _client;
        readonly PubSubOptions _options;

        IDisposable _clientSubscription;
        Channel<IPublishEvent> _messageChannel;

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
            if(_client.IsConnected)
            {
                OnConnected();
            } 
            _clientSubscription = _client.Subscribe(this); // IObservable<>
            _topic = topic;
            _options = options;
             _messageChannel = Channel.CreateUnbounded<IPublishEvent>();
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

        public virtual void OnNext(IPubSubEvent value)
        {
            if (value is IPublishEvent mess)
            {
                _messageChannel.Writer.TryWrite(mess);
            } 
            else if( value is IConnectionEvent conn)
            {
               if(conn.IsConnected)
                {
                    OnConnected();
                }
                else
                {
                    OnDisconnected();
                }
            }
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

        protected async override ValueTask<JsonRpcMessage> ReadCoreAsync(CancellationToken cancellationToken)
        {
            while (await _messageChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_messageChannel.Reader.TryRead(out IPublishEvent e))
                {
                    try
                    {
                        var mess = e.Payload.Length > 0 ? this.Formatter.Deserialize(e.Payload) : null;
                        if (mess is JsonRpcRequest request)
                        {
                            var oldId = request.RequestId;
                            var newId = (RequestIdFactory ?? this).NextRequestId();
                            request.RequestId = newId;
                            _requestIdIndex.TryAdd(newId, (oldId, e.Topic));
                        }
                        return mess;
                    }
                    catch
                    {
                        await WriteErrorAsync(e.Topic.Reverse(), JsonRpcErrorCode.ParseError, Ressources.ExceptionMessages.ParseError, cancellationToken);
                    }
                }
            }
            return default;
        }

        protected override ValueTask WriteCoreAsync(JsonRpcMessage content, CancellationToken cancellationToken) => WriteCoreAsync(content, cancellationToken, null);
        protected async virtual ValueTask WriteCoreAsync(JsonRpcMessage content, CancellationToken cancellationToken, IRpcTopic topic)
        {
            var w = new ArrayBufferWriter<byte>();

            if (content is JsonRpcResult result)
            {
                var id = result.RequestId;
                if (_requestIdIndex.TryGetValue(id, out var cache))
                {
                    result.RequestId = cache.Item1;
                    this.Formatter.Serialize(w, content);
                    var t = cache.Item2.Reverse();
                    if (!await _client.TryPublishAsync(t, new ReadOnlySequence<byte>(w.WrittenMemory), _options?.Publish, cancellationToken))
                    {
                        throw new PubSubException(string.Format(Ressources.ExceptionMessages.PublishFailed, t));
                    }
                }
            }
            else
            {
                var t = topic ?? _topic;
                this.Formatter.Serialize(w, content);
                if (!await _client.TryPublishAsync(t, new ReadOnlySequence<byte>(w.WrittenMemory), _options?.Publish, cancellationToken))
                {
                    throw new PubSubException(string.Format(Ressources.ExceptionMessages.PublishFailed, t));
                }
            }
        }
        
        #endregion

        private async ValueTask WriteErrorAsync(IRpcTopic topic, JsonRpcErrorCode code, string message, CancellationToken cancellationToken)
        {
            var error = new JsonRpcError()
            {
                Error = new JsonRpcError.ErrorDetail() { Code = code, Message = message }
            };
            await WriteCoreAsync(error, cancellationToken, topic);
        }

        private async ValueTask<PubSubJsonRpcMessageHandler> StopAsync()
        {
            try
            {
                if (!await _client.TryUnsubscribeAsync(_topic))
                {
                    throw new PubSubException(string.Format(Ressources.ExceptionMessages.UnsubscribeFailed, _topic));
                }
            }
            finally
            {
                _clientSubscription?.Dispose();
                _clientSubscription = null;
                _messageChannel.Writer.TryComplete();
                _messageChannel = null;
            }
            return this;
        }

        private void OnConnected()
        {
            if (!_client.TrySubscribeAsync(_topic, _options?.Subscribe).GetAwaiter().GetResult())
            {
                throw new PubSubException(string.Format(Ressources.ExceptionMessages.SubscribeFailed, _topic));
            }
        }

        private void OnDisconnected() { }

    }
}
