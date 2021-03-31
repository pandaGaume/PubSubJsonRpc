namespace BlueForest.Messaging.JsonRpc
{
    using System;
    using System.Buffers;
    using System.Threading;
    using System.Threading.Tasks;

    public class PubSubException : Exception
    {
        protected PubSubException(string message)
            : base(message)
        {
        }
        protected PubSubException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// The Pub-Sub Quality Of Service values
    /// </summary>
    public enum PubSubQOS
    {
        AtMostOnce = 0,
        AtLeastOnce = 1,
        ExactlyOnce = 2,
        Reserved = 3
    };

    /// <summary>
    /// The Pub-Sub event interface 
    /// </summary>
    public interface IPubSubJsonRpcPublishEvent
    {
        ReadOnlySequence<byte> Topic { get; }
        ReadOnlySequence<byte> Payload { get; }
    }

    public class PublishOptions
    {
        public const PubSubQOS QualityOfServiceDefault = PubSubQOS.AtLeastOnce;

        public PubSubQOS QualityOfService = QualityOfServiceDefault;
    }

    public class SubscribeOptions
    {
    }

    public class PubSubOptions
    {
        public PublishOptions Publish { get; set; }
        public SubscribeOptions Subscribe { get; set; }
    }

    /// <summary>
    /// The Mqtt interface
    /// </summary>
    public interface IPubSubJsonRpcInterface : IObservable<IPubSubJsonRpcPublishEvent>
    {
        ValueTask UnsubscribeAsync(ReadOnlySequence<byte> topic, CancellationToken cancel = default);
        ValueTask SubscribeAsync(ReadOnlySequence<byte> topic, SubscribeOptions options = null, CancellationToken cancel = default);
        ValueTask PublishAsync(ReadOnlySequence<byte> topic, ReadOnlySequence<byte> payload, PublishOptions options = null, CancellationToken cancel = default);
        ValueTask FlushAsync();
    }
}
