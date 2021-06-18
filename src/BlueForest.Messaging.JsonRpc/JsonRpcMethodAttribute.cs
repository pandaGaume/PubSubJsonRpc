using System;

namespace BlueForest.Messaging.JsonRpc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class JsonRpcMethodNameAttribute : Attribute
    {
        public JsonRpcMethodNameAttribute()
        {
        }

        public JsonRpcMethodNameAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }
}
