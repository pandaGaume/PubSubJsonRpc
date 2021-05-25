using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        Task<bool> Add(string key, IotNode node);
        Task<bool> Remove(string key);
        Task<IotNode> TryGet(string key);
        Task<IEnumerable<IotNode>> Browse();
    }


    public interface IIotHub : IIotCompoundNode
    {
    }
}
