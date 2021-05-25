using Microsoft;
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

    public class JsonRpcPubSubService : IObserver<IPubSubEvent>
    {
        internal readonly IPubSubInterface _pubSubInterface;
        private readonly object _api;
        internal readonly IRpcTopic _rawTopic;
        internal readonly IRpcTopic _listenTopic;
        internal readonly PubSubOptions _options;
        internal ITargetBlock<IPublishEvent> _pipeline;
        internal StreamJsonRpc.JsonRpc _rpc;
        internal IDisposable _internalSubscription;

        public JsonRpcPubSubService(IPubSubInterface pubSubInterface, object api, IRpcTopic topic, PubSubOptions options = null, IJsonRpcMessageFormatter formatter = null)
        {
            Requires.NotNull(pubSubInterface, nameof(pubSubInterface));
            Requires.NotNull(api, nameof(api));
            Requires.NotNull(topic, nameof(topic));

            _pubSubInterface = pubSubInterface;
            _rawTopic = topic;
            _listenTopic = topic.AsSubscribeAny();
            _options = options;
            _api = api;

            // Configure the pipeline
            var largeBufferOptions = new ExecutionDataflowBlockOptions() { BoundedCapacity = 60000 };
            
            // make sure our complete call gets propagated throughout the whole pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            formatter = formatter ?? new JsonMessageFormatter(Encoding.UTF8);
            
            // we avoid parse fatal error which kill the rpc server instance and ignore silently.
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
            }, largeBufferOptions);

            var rpcBlock = new JsonRpcPubSubHandlerBlock(formatter, topic);

            var encoderBlock = new TransformBlock<PUB_SUB_RPC_MESSAGE, IPublishEvent>(rpcMsg =>
            {
                var w = new ArrayBufferWriter<byte>();
                formatter.Serialize(w, rpcMsg.Item1);
                return new PublishEvent()
                {
                    Payload = new ReadOnlySequence<byte>(w.WrittenMemory),
                    Topic = rpcMsg.Item2
                };
            });

            var publishBlock = new ActionBlock<IPublishEvent>(e =>
            {
                _pubSubInterface.TryPublishAsync(e.Topic, e.Payload, _options?.Publish);
            });

            // link the block, filtering message
            decoderBlock.LinkTo(rpcBlock, linkOptions, m => { return m.Item1 != null ; });
            
            decoderBlock.LinkTo(DataflowBlock.NullTarget<PUB_SUB_RPC_MESSAGE>());

            rpcBlock.LinkTo(encoderBlock, linkOptions);
            
            encoderBlock.LinkTo(publishBlock, linkOptions, m => m.Payload.Length != 0 );
            encoderBlock.LinkTo(DataflowBlock.NullTarget<IPublishEvent>());

            _pipeline = decoderBlock;

            // Configure Json RPC
            _rpc = new StreamJsonRpc.JsonRpc(rpcBlock);
            _rpc.AddLocalRpcTarget(_api);
            _rpc.Disconnected += _rpc_Disconnected;
            _rpc.StartListening();
#if DEBUG
            _rpc.TraceSource.Switch.ShouldTrace(TraceEventType.Verbose);
            _rpc.TraceSource.Listeners.Add(new ConsoleTraceListener());
#endif
            _internalSubscription = _pubSubInterface.Subscribe(this);
            if (_pubSubInterface.IsConnected)
            {
                _ = OnBrokerConnectedAsync();
            }
        }


        public StreamJsonRpc.JsonRpc RPC => _rpc;

        private async ValueTask OnBrokerConnectedAsync()
        {
            if (! await _pubSubInterface.TrySubscribeAsync(_listenTopic, _options?.Subscribe))
            {
                throw new PubSubException(string.Format(Ressources.ExceptionMessages.SubscribeFailed, _listenTopic));
            }
        }

        private ValueTask OnBrokerDisconnectedAsync() => default;

        #region IObserver<IPubSubEvent>
        public void OnCompleted()
        {
            _pipeline?.Complete();
        }

        public void OnError(Exception error)
        {
            _pipeline?.Fault(error);
        }

        public void OnNext(IPubSubEvent value)
        {
            if (value is IPublishEvent pe)
            {
                _pipeline.Post(pe);
                return;
            } 
            
            if( value is IConnectionEvent ce)
            {
                if (ce.IsConnected) _ = OnBrokerConnectedAsync(); 
                else _ = OnBrokerDisconnectedAsync();
            }
        }
        #endregion
        private void _rpc_Disconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
        }

    }
}
