
namespace BlueForest.Messaging.JsonRpc
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A base abstract class to implement the Pub/Sub interface with the IObservable<IPubSubJsonRpcPublishEvent> logic.
    /// </summary>
    public abstract class AbstractPubSubInterface : IPubSubInterface
    {
        readonly List<IObserver<IPubSubEvent>> _observers;

 
        public AbstractPubSubInterface(List<IObserver<IPubSubEvent>> container = null)
        {
            _observers = container?? new List<IObserver<IPubSubEvent>>(1);
        }

        #region IObservable<IPubSubJsonRpcPublishEvent>
        public IDisposable Subscribe(IObserver<IPubSubEvent> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new Unsubscriber<IPubSubEvent>(_observers, observer);
        }
        #endregion

        protected void OnEvent(IPubSubEvent e)
        {
            foreach(var o in _observers)
            {
                o.OnNext(e);
            }
        }

        public abstract bool IsConnected { get; }
        public abstract ValueTask<bool> TryPublishAsync(IRpcTopic topic, ReadOnlySequence<byte> payload, PublishOptions options = null, CancellationToken cancel = default);
        public abstract ValueTask<bool> TrySubscribeAsync(IRpcTopic topic, SubscribeOptions options = null, CancellationToken cancel = default);
        public abstract ValueTask<bool> TryUnsubscribeAsync(IRpcTopic topic, CancellationToken cancel = default);
    }
}
