namespace BlueForest.Messaging.JsonRpc
{
    public class JsonRpcSettings
    {
        public JsonRpcBrokerSession[] Sessions { get; set; }
        public JsonRpcRouteInfo ControllerRoute { get; set; }
        public JsonRpcRouteInfo ServiceRoute { get; set; }
    }
}
