
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
    public abstract class AbstractPubSubJsonRpc : IPubSubJsonRpcInterface
    {
        readonly List<IObserver<IApplicationMessage>> _observers;

        public AbstractPubSubJsonRpc(List<IObserver<IApplicationMessage>> container = null)
        {
            _observers = container?? new List<IObserver<IApplicationMessage>>(1);
        }

        #region IObservable<IPubSubJsonRpcPublishEvent>
        public IDisposable Subscribe(IObserver<IApplicationMessage> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new Unsubscriber<IApplicationMessage>(_observers, observer);
        }
        #endregion

        protected void FirePublishEvent(IApplicationMessage e)
        {
            foreach(var o in _observers)
            {
                o.OnNext(e);
            }
        }

        public abstract ValueTask PublishAsync(IRpcTopic topic, ReadOnlySequence<byte> payload, PublishOptions options = null, CancellationToken cancel = default);
        public abstract ValueTask SubscribeAsync(IRpcTopic topic, SubscribeOptions options = null, CancellationToken cancel = default);
        public abstract ValueTask UnsubscribeAsync(IRpcTopic topic, CancellationToken cancel = default);
    }
}
