using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Samples.Commons
{
    public class IotNode : IIotNode
    {
        string _id;
        string _dn;
        string _desc;

        public IotNode() { }
        public IotNode(string id, string displayName = null, string description = null) 
        {
            _id = id;
            _dn = displayName;
            _desc = description;
        }

        public string Id { get => _id; set => _id = value; }
        public string DisplayName { get => _dn; set => _dn = value; }
        public string Description { get => _desc; set => _desc = value; }
    }

    public class IotHub : IIOtHub
    {
        readonly ConcurrentDictionary<string, IIotNode> _storage ;

        public IotHub()
        {
            _storage = new ConcurrentDictionary<string, IIotNode>() ;
        }
        public IotHub(IEnumerable<IIotNode> nodes) : this()
        {
            foreach(var n in nodes)
            {
                _storage?.TryAdd(n.Id, n);
            }
        }

        public IEnumerable<IIotNode> Browse() => _storage.Select(p=>p.Value);
        public bool TryAdd(string key, IIotNode node) => _storage.TryAdd(key, node);
        public bool TryGet(string key, out IIotNode node) => _storage.TryGetValue(key, out node);
        public bool TryRemove(string key, out IIotNode node) => _storage.TryRemove(key, out node);
    }
}
