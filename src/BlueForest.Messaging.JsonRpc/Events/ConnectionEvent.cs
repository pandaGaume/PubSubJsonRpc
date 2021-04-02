namespace BlueForest.Messaging.JsonRpc
{
    using System.Buffers;
    public class ConnectionEvent : IConnectionEvent
    {
        public bool IsConnected { get; set; }
    }
}
