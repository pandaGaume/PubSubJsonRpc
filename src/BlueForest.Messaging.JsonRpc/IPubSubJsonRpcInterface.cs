namespace BlueForest.Messaging.JsonRpc
{
    using System;
    using System.Buffers;
    using System.Threading;
    using System.Threading.Tasks;

    [Serializable]
    public class PubSubException : Exception
    {
        public PubSubException(string message)
            : base(message)
        {
        }
        public PubSubException(string message, Exception innerException)
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
    public interface IPubSubEvent
    {
    }

    public interface IPublishEvent : IPubSubEvent
    {
        IRpcTopic Topic { get; }
        ReadOnlySequence<byte> Payload { get; }
    }

    public interface IConnectionEvent : IPubSubEvent
    {
        bool IsConnected { get; set; }
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

    public interface IRpcTopic
    {
        ReadOnlyMemory<byte> Path { get; set; }
        ReadOnlyMemory<byte> From { get; set; }
        ReadOnlyMemory<byte> To { get; set; }
        IRpcTopic Reverse(); 
    }

    /// <summary>
    /// The Mqtt interface
    /// </summary>
    public interface IPubSubJsonRpcInterface : IObservable<IPubSubEvent>
    {
        bool IsConnected { get; }
        ValueTask<bool> TryUnsubscribeAsync(IRpcTopic topic, CancellationToken cancel = default);
        ValueTask<bool> TrySubscribeAsync(IRpcTopic topic, SubscribeOptions options = null, CancellationToken cancel = default);
        ValueTask<bool> TryPublishAsync(IRpcTopic topic, ReadOnlySequence<byte> payload, PublishOptions options = null, CancellationToken cancel = default);
    }
}
