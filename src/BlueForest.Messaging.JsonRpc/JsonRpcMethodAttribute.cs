using System;

namespace BlueForest.Messaging.JsonRpc
{
    [AttributeUsage(AttributeTargets.Method)]
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
