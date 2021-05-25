using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc
{
    public partial class JsonRpcPubSubHandlerBlock :IRequestIdFactory
    {
        public static long IdFactorySeedDefault = 0;

        
        long _id = IdFactorySeedDefault;
        readonly long _seed = IdFactorySeedDefault;
        internal IRequestIdFactory _requestIdFactory;
        internal readonly ConcurrentDictionary<RequestId, (RequestId, IRpcTopic)> _requestIdIndex = new ConcurrentDictionary<RequestId, (RequestId, IRpcTopic)>();

        public IRequestIdFactory RequestIdFactory
        {
            get => _requestIdFactory?? this;
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

    }
}
