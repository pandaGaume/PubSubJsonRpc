using Microsoft;
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
        readonly internal IRpcTopic _requestTopic; // used for write request

        public JsonRpcPubSubHandlerBlock(IRpcTopic requestTopic, IJsonRpcMessageFormatter formatter) : this(requestTopic, formatter, new DataflowBlockOptions())
        {
        }
 
        public JsonRpcPubSubHandlerBlock(IRpcTopic topic, IJsonRpcMessageFormatter formatter, DataflowBlockOptions dataflowBlockOptions) : base(formatter)
        {
            _target = new BufferBlock<PUB_SUB_RPC_MESSAGE>(dataflowBlockOptions);
            _source = new BufferBlock<PUB_SUB_RPC_MESSAGE>(dataflowBlockOptions);
            _ = _target.Completion.ContinueWith(delegate
            {
                _source?.Complete();
            }, TaskScheduler.Default);
            _requestTopic = topic;
        }
        public override bool CanRead => true;
        public override bool CanWrite => true;
        protected override async ValueTask<JsonRpcMessage> ReadCoreAsync(CancellationToken cancellationToken)
        {
            while (await _target.OutputAvailableAsync(cancellationToken))
            {
                var mess = await _target.ReceiveAsync();

                if (mess.Item1 is JsonRpcRequest request)
                {
                    var oldId = request.RequestId;
                    var newId = (RequestIdFactory ?? this).NextRequestId();
                    request.RequestId = newId;
                    _requestIdIndex.TryAdd(newId, (oldId, mess.Item2));
                }
                return mess.Item1;
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
                        if (!cache.Item1.IsEmpty)
                        {
                            error.RequestId = cache.Item1;
                            _source.Post(new Tuple<JsonRpcMessage, IRpcTopic>(c, topic));
                        }
                    }
                } 
                else if (c is JsonRpcRequest requet)
                {
                    _source.Post(new Tuple<JsonRpcMessage, IRpcTopic>(c, _requestTopic));
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
        internal ISourceBlock<PUB_SUB_RPC_MESSAGE> Source => _source;
        internal ITargetBlock<PUB_SUB_RPC_MESSAGE> Target => _target;
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
