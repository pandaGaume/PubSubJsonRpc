using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public class IotHub : IIotHub
    {
        readonly ConcurrentDictionary<string, IotNode> _storage ;

        public IotHub()
        {
            _storage = new ConcurrentDictionary<string, IotNode>() ;
        }
        public IotHub(IEnumerable<IotNode> nodes) : this()
        {
            foreach(var n in nodes)
            {
                _storage?.TryAdd(n.Id, n);
            }
        }

        public Task<IEnumerable<IotNode>> Browse() => Task.FromResult(_storage.Select(p=>p.Value));
        public Task<bool> Add(string key, IotNode node) => Task.FromResult(_storage.TryAdd(key, node));
        public Task<IotNode> TryGet(string key)
        {
            if (_storage.TryGetValue(key, out var node))
            {
                return Task.FromResult(node);
            }
            return null;
        }
        public Task<bool> Remove(string key) => Task.FromResult(_storage.TryRemove(key, out var node));
    }
}
