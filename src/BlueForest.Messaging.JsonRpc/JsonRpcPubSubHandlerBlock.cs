using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc
{
    using PUB_SUB_RPC_MESSAGE = Tuple<JsonRpcMessage, IRpcTopic>;

    public partial class JsonRpcPubSubHandlerBlock : MessageHandlerBase, IPropagatorBlock<PUB_SUB_RPC_MESSAGE, PUB_SUB_RPC_MESSAGE>
    {
        internal static RequestId Unknown = new RequestId("unknown");

        readonly internal BufferBlock<PUB_SUB_RPC_MESSAGE> _target;
        readonly internal BufferBlock<PUB_SUB_RPC_MESSAGE> _source;
        internal JsonRpcPubSubOptions _options;

        public JsonRpcPubSubHandlerBlock(JsonRpcPubSubOptions options, IJsonRpcMessageFormatter formatter) : this(options, formatter, new DataflowBlockOptions())
        {
        }

        public JsonRpcPubSubHandlerBlock(JsonRpcPubSubOptions options, IJsonRpcMessageFormatter formatter, DataflowBlockOptions dataflowBlockOptions) : base(formatter)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _target = new BufferBlock<PUB_SUB_RPC_MESSAGE>(dataflowBlockOptions);
            _source = new BufferBlock<PUB_SUB_RPC_MESSAGE>(dataflowBlockOptions);
            _ = _target.Completion.ContinueWith(delegate
            {
                _source?.Complete();
            }, TaskScheduler.Default);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
        protected override async ValueTask<JsonRpcMessage> ReadCoreAsync(CancellationToken cancellationToken)
        {
            while (await _target.OutputAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                var mess = await _target.ReceiveAsync();

                if (mess.Item1 is JsonRpcRequest request)
                {
                    var oldId = request.RequestId;
                    var newId = (RequestIdFactory ?? this).NextRequestId();
                    request.RequestId = newId;
                    _requestIdIndex.TryAdd(newId, (oldId, mess.Item2));
                    return mess.Item1;
                }
                else if (mess.Item1 is JsonRpcResult result)
                {
                    if (_requestCache.TryGetValue(result.RequestId, out var id))
                    {
                        _requestCache.Remove(id);
                        return mess.Item1;
                    }
                }
                else if (mess.Item1 is JsonRpcError error)
                {
                    if (_requestCache.TryGetValue(error.RequestId, out var id))
                    {
                        _requestCache.Remove(id);
                        return mess.Item1;
                    }

                    try
                    {
                        await _evictedLock.WaitAsync(cancellationToken);
                        if (_evicted.Contains(error.RequestId))
                        {
                            _evicted.Remove(error.RequestId);
                            return mess.Item1;
                        }
                    }
                    finally
                    {
                        _evictedLock.Release();
                    }
                }
            }
            return default;
        }
        protected override ValueTask WriteCoreAsync(JsonRpcMessage content, CancellationToken cancellationToken)
        {
            var c = content;

            RequestId id = default;
            try
            {
                if (c is JsonRpcResult result)
                {
                    id = result.RequestId;
                    if (_requestIdIndex.TryGetValue(id, out var cache))
                    {
                        IRpcTopic topic = cache.Item2.ReverseInPlace();
                        topic.Channel = _options.Topics.Response.Channel;
                        if (!cache.Item1.IsEmpty)
                        {
                            result.RequestId = cache.Item1;
                            _source.Post(new Tuple<JsonRpcMessage, IRpcTopic>(c, topic));
                        }
                    }
                }
                else if (c is JsonRpcError error)
                {
                    id = error.RequestId;
                    if (_requestIdIndex.TryGetValue(id, out var cache))
                    {
                        IRpcTopic topic = cache.Item2.ReverseInPlace();
                        topic.Channel = _options.Topics.Response.Channel;
                        if (!cache.Item1.IsEmpty)
                        {
                            error.RequestId = cache.Item1;
                            _source.Post(new Tuple<JsonRpcMessage, IRpcTopic>(c, topic));
                        }
                    }
                }
                else if (c is JsonRpcRequest request)
                {
                    if (request.IsResponseExpected)
                    {
                        var cacheEntryOptions = new MemoryCacheEntryOptions().RegisterPostEvictionCallback(OnPostEviction);
                        cacheEntryOptions.AddExpirationToken(new CancellationChangeToken(cancellationToken));
                        _requestCache.Set(request.RequestId, request.RequestId, cacheEntryOptions);
                    }
                    _source.Post(new Tuple<JsonRpcMessage, IRpcTopic>(c, request.IsNotification ? _options.Topics.Notification : _options.Topics.Request));
                }
            }
            finally
            {
                if (!_requestIdIndex.TryRemove(id, out var cache))
                {
                    // something went wrong internally
                }
            }

            return new ValueTask();
        }
        protected override ValueTask FlushAsync(CancellationToken cancellationToken) => default;

        public void PostBackRequestError(RequestId id, JsonRpcErrorCode code = JsonRpcErrorCode.InternalError, string message = null)
        {
            JsonRpcMessage m = new JsonRpcError()
            {
                RequestId = id,
                Error = new JsonRpcError.ErrorDetail() { Code = code, Message = message }
            };
            _target.Post(new Tuple<JsonRpcMessage, IRpcTopic>(m, null));
#if DEBUG
            Console.WriteLine($"PosBack Error : {m}");
#endif
        }

        internal ISourceBlock<PUB_SUB_RPC_MESSAGE> Source => _source;
        internal ITargetBlock<PUB_SUB_RPC_MESSAGE> Target => _target;
        public JsonRpcPubSubOptions Options => _options;
        public Task Completion => _source.Completion;
        public PUB_SUB_RPC_MESSAGE ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<PUB_SUB_RPC_MESSAGE> target, out bool messageConsumed) => Source.ConsumeMessage(messageHeader, target, out messageConsumed);
        public IDisposable LinkTo(ITargetBlock<PUB_SUB_RPC_MESSAGE> target, DataflowLinkOptions linkOptions) => _source.LinkTo(target, linkOptions);
        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<PUB_SUB_RPC_MESSAGE> target) => Source.ReleaseReservation(messageHeader, target);
        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<PUB_SUB_RPC_MESSAGE> target) => Source.ReserveMessage(messageHeader, target);
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, PUB_SUB_RPC_MESSAGE messageValue, ISourceBlock<PUB_SUB_RPC_MESSAGE> source, bool consumeToAccept) => Target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        public void Complete() => _target.Complete();
        public void Fault(Exception exception) => Target.Fault(exception);
    }
}
