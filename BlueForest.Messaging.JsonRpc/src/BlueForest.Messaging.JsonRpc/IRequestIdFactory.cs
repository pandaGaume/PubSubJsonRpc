namespace BlueForest.Messaging.JsonRpc
{
    using StreamJsonRpc;

    /// <summary>
    /// The factory interface used to create new Request Id for server logic.
    /// </summary>
    public interface IRequestIdFactory
    {
        RequestId NextRequestId();
    }
}
