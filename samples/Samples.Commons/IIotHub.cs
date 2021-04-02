using System;
using System.Collections.Generic;

namespace Samples.Commons
{

    public interface IIotNode
    {
        string Id { get; set; }
        string DisplayName { get; set; }
        string Description { get; set; }
    }


    public interface IIotCompoundNode
    {
        bool TryAdd(string key, IIotNode node);
        bool TryRemove(string key, out IIotNode node);
        bool TryGet(string key, out IIotNode node);
        IEnumerable<IIotNode> Browse();
    }


    public interface IIOtHub : IIotCompoundNode
    {
    }
}
