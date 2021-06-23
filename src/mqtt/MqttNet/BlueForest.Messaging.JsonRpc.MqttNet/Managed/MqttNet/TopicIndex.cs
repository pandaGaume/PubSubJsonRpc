using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlueForest.Messaging.MqttNet
{
    public class TopicIndex<T> : TopicIndexNode<T>
    {
        const char Separator = '/';
        const string SingleLevelWildChar = "+";
        const string MultiLevelWildChar = "*";

        public TopicIndex() : base("__root__") { }

        public void Bind(string topic, IEnumerable<T> data) => Bind(topic?.Split(Separator), data);
        public void Bind(string[] parts, IEnumerable<T> data)
        {
            if (parts == null) throw new ArgumentNullException(nameof(parts));
            if (data == null) throw new ArgumentNullException(nameof(data));

            TopicIndexNode<T> node = this;
            for (int i = 0; i != parts.Length; i++)
            {
                if (!node.TryGetValue(parts[i], out node))
                {
                    var tmp = new TopicIndexNode<T>(parts[i]);
                    node.Add(tmp);
                    node = tmp;
                }
            }
            node.Add(data);
        }

        public void Unbind(string topic, IEnumerable<T> data) => Unbind(topic?.Split(Separator), data);
        public void Unbind(string[] parts, IEnumerable<T> data)
        {
            if (parts == null) throw new ArgumentNullException(nameof(parts));
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (parts.Length != 0)
            {
                TopicIndexNode<T> node = this;
                Queue<TopicIndexNode<T>> path = new Queue<TopicIndexNode<T>>(parts.Length);
                for (int i = 0; i != parts.Length; i++)
                {
                    if (!node.TryGetValue(parts[i], out node))
                    {
                        return;
                    }
                    path.Enqueue(node);
                }
                node.Remove(data);
                
                // clean the tree
                if (path.Count != 0)
                {
                    var parent = path.Dequeue();
                    do
                    {
                        node = parent;
                        if (node.HasChildrens || node.HasData)
                        {
                            break;
                        }
                        parent = path.Count == 0 ? this : path.Dequeue();
                        parent.Remove(node);
                    } while (path.Count != 0);
                }
            }
        }

        public IEnumerable<T> Lookup(string topic) => Lookup(topic?.Split(Separator));
        public IEnumerable<T> Lookup(string[] parts) => Lookup(parts, 0, parts?.Length ?? 0);
        internal IEnumerable<T> Lookup(string[] parts, int from, int length)
        {
            if (parts == null) throw new ArgumentNullException(nameof(parts));

            TopicIndexNode<T> node = this;
            if (length > 0)
            {
                do
                {
                    var k = node._key;
                    if (k == SingleLevelWildChar)
                    {
                        length--;
                        if (node.TryGetValue(parts[++from], out node))
                        {
                            continue;
                        }
                        break;
                    }

                    if (k == MultiLevelWildChar)
                    {
                        return node._datas?? Enumerable.Empty<T>();
                    }

                    if (k == parts[from])
                    {
                        length--;
                        if (length == 0)
                        {
                            return node._datas?? Enumerable.Empty<T>();
                        }
                        if (node.TryGetValue(parts[++from], out node))
                        {
                            continue;
                        }
                        break;
                    }
                } while (length > 0);
            }
            return Enumerable.Empty<T>();
        }
    }

}
