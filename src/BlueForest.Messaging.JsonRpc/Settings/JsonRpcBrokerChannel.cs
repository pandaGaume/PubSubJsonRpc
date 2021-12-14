using System;
using System.Collections.Generic;
using System.Text;

namespace BlueForest.Messaging.JsonRpc
{
    public enum JsonRpcBrokerChannelType
    {
        R, W, RW
    }

    public class JsonRpcBrokerChannel
    {
        JsonRpcBrokerChannelType _t = JsonRpcBrokerChannelType.RW;
        string _n;

        public string Name { get => _n; set => _n = value; }
        public JsonRpcBrokerChannelType Type { get => _t; set => _t = value; }
    }
}
