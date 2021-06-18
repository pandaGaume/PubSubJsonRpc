using Microsoft.Extensions.Caching.Memory;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using System.Collections.Generic;
using System.Threading;

namespace BlueForest.Messaging.JsonRpc
{
    public partial class JsonRpcPubSubHandlerBlock
    {
        MemoryCache _requestCache = new MemoryCache(new MemoryCacheOptions());
        List<RequestId> _evicted = new List<RequestId>(2);
        SemaphoreSlim _evictedLock = new SemaphoreSlim(1);

        private void OnPostEviction(object key, object value, EvictionReason reason, object state)
        {
            if (reason == EvictionReason.Expired || reason == EvictionReason.TokenExpired)
            {
                var v = (RequestId)value;
                try
                {
                    _evictedLock.Wait();
                    _evicted.Add(v);
                }
                finally
                {
                    _evictedLock.Release();
                }
                PostBackRequestError(v, JsonRpcErrorCode.RequestCanceled);
            }
        }
    }
}
