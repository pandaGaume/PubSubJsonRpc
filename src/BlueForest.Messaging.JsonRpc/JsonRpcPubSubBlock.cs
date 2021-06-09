using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc
{
    using PUB_SUB_RPC_MESSAGE = Tuple<JsonRpcMessage, IRpcTopic>;


    public class JsonRpcPubSubBlock : IJsonRpcPubSub, IDisposable
    {
        internal StreamJsonRpc.JsonRpc _rpc;
        internal ITargetBlock<IPublishEvent> _target;
        internal JsonRpcPubSubHandlerBlock _handler;
        internal ISourceBlock<IPublishEvent> _source;

        private bool disposed;

        public JsonRpcPubSubBlock(JsonRpcPubSubTopics topics, IJsonRpcMessageFormatter formatter = null)
        {
            // Configure the pipeline
            var smallBufferOptions = new ExecutionDataflowBlockOptions() { BoundedCapacity = 1000 };
            
            // make sure our complete call gets propagated throughout the whole pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            // build a formatter for jsonRPC
            formatter = formatter ?? new JsonMessageFormatter(Encoding.UTF8);
            
            // thanks to decoder block, we avoid parse fatal error which kill the rpc server instance and ignore silently.
            var decoderBlock = new TransformBlock<IPublishEvent, PUB_SUB_RPC_MESSAGE>((publishEvent) =>
            {
                var jsonAsByteSequence = publishEvent.Payload;
                JsonRpcMessage rpcMsg = null;
                try
                {
                    if (jsonAsByteSequence.Length > 0)
                    {
                        rpcMsg = formatter.Deserialize(jsonAsByteSequence);
                    }
                }
                catch
                {
                }
                return new Tuple<JsonRpcMessage, IRpcTopic>(rpcMsg, publishEvent.Topic);
            }, smallBufferOptions);

            // the rpc handler
            _handler = new JsonRpcPubSubHandlerBlock(topics, formatter);

            // the encoder
            var encoderBlock = new TransformBlock<PUB_SUB_RPC_MESSAGE, IPublishEvent>(rpcMsg =>
            {
                var w = new ArrayBufferWriter<byte>();
                formatter.Serialize(w, rpcMsg.Item1);
                var pe = new PublishEvent()
                {
                    Payload = new ReadOnlySequence<byte>(w.WrittenMemory),
                    Topic = rpcMsg.Item2
                };
                if(rpcMsg.Item1 is JsonRpcRequest request)
                {
                    pe.RequestId = request.RequestId;
                    pe.PublishType = request.IsNotification ? PublishType.Notification : PublishType.Request;
                }
                else if (rpcMsg.Item1 is JsonRpcResult result)
                {
                    pe.RequestId = result.RequestId;
                    pe.PublishType = PublishType.Response;
                }
                else if (rpcMsg.Item1 is JsonRpcError error)
                {
                    pe.RequestId = error.RequestId;
                    pe.PublishType = PublishType.Error;
                }

                return pe;
            });

            // link the block, filtering message.
            // Note we provide a NullTarget to lets the block waste message which do not pass the predicates
            decoderBlock.LinkTo(_handler, linkOptions, m => { return m.Item1 != null ; });
            decoderBlock.LinkTo(DataflowBlock.NullTarget<PUB_SUB_RPC_MESSAGE>());

            _handler.LinkTo(encoderBlock, linkOptions, m => { return m.Item1 != null; });
            _handler.LinkTo(DataflowBlock.NullTarget<PUB_SUB_RPC_MESSAGE>());

            _target = decoderBlock;
            _source = encoderBlock;

            // Configure Json RPC
            InitializeRpc();
        }

        public ITargetBlock<PUB_SUB_RPC_MESSAGE> LocalTarget => _handler;

        // used for server side
        public void AddLocalRpcTarget(object target) => _rpc?.AddLocalRpcTarget(target);
        // used for client side
        public object Attach(Type api) => _rpc?.Attach(api);
        public T Attach<T>() where T : class => _rpc?.Attach<T>();
        public void StartListening() => _rpc?.StartListening();
        public StreamJsonRpc.JsonRpc RPC => _rpc;

        private void _rpc_Disconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            InitializeRpc();
        }

        protected virtual void InitializeRpc()
        {
            _rpc = new StreamJsonRpc.JsonRpc(_handler);
            _rpc.Disconnected += _rpc_Disconnected;
#if DEBUG
            _rpc.TraceSource.Switch.ShouldTrace(TraceEventType.Verbose);
            _rpc.TraceSource.Listeners.Add(new ConsoleTraceListener());
#endif
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _target.Complete();
                    _rpc?.Dispose();
                }
                disposed = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal ISourceBlock<IPublishEvent> Source => _source;
        internal ITargetBlock<IPublishEvent> Target => _target;
        public Task Completion => _target.Completion;
        public IPublishEvent ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<IPublishEvent> target, out bool messageConsumed) => Source.ConsumeMessage(messageHeader, target, out messageConsumed);
        public IDisposable LinkTo(ITargetBlock<IPublishEvent> target, DataflowLinkOptions linkOptions) => _source.LinkTo(target, linkOptions);
        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<IPublishEvent> target) => Source.ReleaseReservation(messageHeader, target);
        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<IPublishEvent> target) => Source.ReserveMessage(messageHeader, target);
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, IPublishEvent messageValue, ISourceBlock<IPublishEvent> source, bool consumeToAccept) => Target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        public void Complete() => _target.Complete();
        public void Fault(Exception exception) => Target.Fault(exception);
    }
}
