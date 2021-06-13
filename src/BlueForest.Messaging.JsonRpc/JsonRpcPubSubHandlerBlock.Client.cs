using Microsoft.Extensions.Caching.Memory;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc
{
    public partial class JsonRpcPubSubHandlerBlock 
    {
        IMemoryCache _requestCache = new MemoryCache(new MemoryCacheOptions());
        internal MemoryCacheEntryOptions _cacheEntryOptions;
        SemaphoreSlim _cacheLock;

        private void OnPostEviction(object key, object value, EvictionReason reason, object state)
        {
            if (reason == EvictionReason.Expired || reason == EvictionReason.TokenExpired)
            {
                PostBackRequestError((RequestId)value);
            }
        }
    }
}
