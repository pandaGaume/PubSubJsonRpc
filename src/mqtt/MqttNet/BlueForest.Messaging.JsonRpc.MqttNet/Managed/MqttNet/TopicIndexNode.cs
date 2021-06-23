using System;
using System.Collections.Generic;
using System.Text;

namespace BlueForest.Messaging.MqttNet
{
    public class TopicIndexNode<T>
    {
        internal string _key;
        internal IDictionary<string, TopicIndexNode<T>> _childrens;
        internal List<T> _datas;

        internal TopicIndexNode(string key)
        {
            _key = new string(key);
        }

        internal bool HasChildrens => _childrens != null;
        internal bool HasData => _datas != null;

        internal bool TryGetValue(string key, out TopicIndexNode<T> child)
        {
            child = null;
            return _childrens?.TryGetValue(key, out child) ?? false;
        }
        internal void Add(TopicIndexNode<T> child)
        {
            _childrens = _childrens ?? new Dictionary<string, TopicIndexNode<T>>();
            _childrens.Add(child._key, child);
        }
        internal void Remove(TopicIndexNode<T> child)
        {
            if (_childrens != null)
            {
                _childrens.Remove(child._key);
                if (_childrens.Count == 0)
                {
                    _childrens = null;
                }
            }
        }
        internal void Add(IEnumerable<T> data)
        {
            _datas = _datas ?? new List<T>(2);
            _datas.AddRange(data);
        }
        internal void Remove(IEnumerable<T> data)
        {
            if (_datas != null)
            {
                foreach (var o in data)
                {
                    _datas.Remove(o);
                }
                if (_datas.Count == 0)
                {
                    _datas = null;
                }
            }
        }
    }
}
