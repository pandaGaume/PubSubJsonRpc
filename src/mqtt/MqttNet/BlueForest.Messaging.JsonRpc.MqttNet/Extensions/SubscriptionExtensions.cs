using System.Collections.Generic;
using System.Linq;

namespace BlueForest.Messaging.JsonRpc.MqttNet
{
    public static class SubscriptionExtensions
    {
        public static IEnumerable<JsonRpcSubscription> Match(this IEnumerable<JsonRpcSubscription> subscriptions, IRpcTopic topic)
        {
            return subscriptions.Where(s => s?.Topic?.Match(topic) ?? false);
        }
    }
}
