using System;
using System.Threading.Tasks.Dataflow;

namespace BlueForest.Messaging.JsonRpc
{
    public interface IJsonRpcPubSub : IPropagatorBlock<IPublishEvent, IPublishEvent>
    {
        void AddLocalRpcTarget(object target);
        object Attach(Type api);
        T Attach<T>() where T : class;
        void StartListening();
        StreamJsonRpc.JsonRpc RPC { get; }
    }
}
