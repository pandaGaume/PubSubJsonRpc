using StreamJsonRpc;
using System;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc
{
    public interface IJsonRpcPubSub : IPropagatorBlock<IPublishEvent, IPublishEvent>
    {
        void AddLocalRpcTarget(object target, JsonRpcTargetOptions options = null);
        object Attach(Type api, JsonRpcProxyOptions options = null);
        T Attach<T>(JsonRpcProxyOptions options = null) where T : class;
        void StartListening();
    }
}
