
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
        readonly List<IObserver<IPubSubJsonRpcPublishEvent>> _observers;

        public AbstractPubSubJsonRpc(List<IObserver<IPubSubJsonRpcPublishEvent>> container = null)
        {
            _observers = container?? new List<IObserver<IPubSubJsonRpcPublishEvent>>(1);
        }

        #region IObservable<IPubSubJsonRpcPublishEvent>
        public IDisposable Subscribe(IObserver<IPubSubJsonRpcPublishEvent> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new Unsubscriber<IPubSubJsonRpcPublishEvent>(_observers, observer);
        }
        #endregion

        public abstract ValueTask FlushAsync();
        public abstract ValueTask PublishAsync(ReadOnlySequence<byte> topic, ReadOnlySequence<byte> payload, PublishOptions options = null, CancellationToken cancel = default);
        public abstract ValueTask SubscribeAsync(ReadOnlySequence<byte> topic, SubscribeOptions options = null, CancellationToken cancel = default);
        public abstract ValueTask UnsubscribeAsync(ReadOnlySequence<byte> topic, CancellationToken cancel = default);

    }
}
