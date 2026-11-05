using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>Generic object pool for components. Released items are deactivated and reparented.</summary>
    public sealed class Pool<T> where T : Component
    {
        readonly Stack<T> free = new Stack<T>();
        readonly Func<T> create;
        readonly Transform root;

        public Pool(Func<T> create, Transform root)
        {
            this.create = create;
            this.root = root;
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
                Release(create());
        }

        public T Get()
        {
            var item = free.Count > 0 ? free.Pop() : create();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(root, false);
            free.Push(item);
        }
    }
}
