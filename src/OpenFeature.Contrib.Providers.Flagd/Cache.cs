using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace OpenFeature.Contrib.Providers.Flagd
{
    internal interface ICache<TKey, TValue>
    {
        void Add(TKey key, TValue value);
        TValue TryGet(TKey key);
        void Delete(TKey key);
        void Purge();
    }
    class LRUCache<TKey, TValue> : ICache<TKey, TValue> where TValue : class
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, Node> _map;
        private Node _head;
        private Node _tail;

        private System.Threading.Mutex _mtx;

        public LRUCache(int capacity)
        {
            _capacity = capacity;
            _map = new Dictionary<TKey, Node>();
            _mtx = new System.Threading.Mutex();
        }

        public TValue TryGet(TKey key)
        {
            using (var mtx = new Mutex(ref _mtx))
            {
                mtx.Lock();
                if (_map.TryGetValue(key, out Node node))
                {
                    MoveToFront(node);
                    return node.Value;
                }
                return default(TValue);
            }
        }

        public void Add(TKey key, TValue value)
        {
            using (var mtx = new Mutex(ref _mtx))
            {
                mtx.Lock();
                if (_map.TryGetValue(key, out Node node))
                {
                    node.Value = value;
                    MoveToFront(node);
                }
                else
                {
                    if (_map.Count >= _capacity)
                    {
                        _map.Remove(_tail.Key);
                        RemoveTail();
                    }
                    node = new Node(key, value);
                    _map.Add(key, node);
                    AddToFront(node);
                }
            }
        }

        public void Delete(TKey key)
        {
            using (var mtx = new Mutex(ref _mtx))
            {
                mtx.Lock();
                if (_map.TryGetValue(key, out Node node))
                {
                    if (node == _head)
                    {
                        _head = node.Next;
                    }
                    else
                    {
                        node.Prev.Next = node.Next;
                    }
                    if (node.Next != null)
                    {
                        node.Next.Prev = node.Prev;
                    }
                    _map.Remove(key);
                }
            }
        }

        public void Purge()
        {
            using (var mtx = new Mutex(ref _mtx))
            {
                mtx.Lock();
                _map.Clear();
            }
        }

        private void MoveToFront(Node node)
        {
            if (node == _head)
                return;
            node.Prev.Next = node.Next;
            if (node == _tail)
                _tail = node.Prev;
            else
                node.Next.Prev = node.Prev;
            AddToFront(node);
        }

        private void AddToFront(Node node)
        {
            if (_head == null)
            {
                _head = node;
                _tail = node;
                return;
            }
            node.Next = _head;
            _head.Prev = node;
            _head = node;
        }

        private void RemoveTail()
        {
            _tail = _tail.Prev;
            if (_tail != null)
                _tail.Next = null;
            else
                _head = null;
        }

        private class Node
        {
            public TKey Key;
            public TValue Value;
            public Node Next;
            public Node Prev;

            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private class Mutex : System.IDisposable
        {

            public System.Threading.Mutex _mtx;

            public Mutex(ref System.Threading.Mutex mtx)
            {
                _mtx = mtx;
            }

            public void Lock()
            {
                _mtx.WaitOne();
            }

            public void Dispose()
            {
                _mtx.ReleaseMutex();
            }
        }
    }

}

