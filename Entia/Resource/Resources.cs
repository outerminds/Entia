using Entia.Core;
using Entia.Core.Documentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    [ThreadSafe]
    public sealed class Resources : IModule, IClearable, IEnumerable<IResource>
    {
        static readonly object _key = typeof(Resources);

        readonly Boxes _boxes;
        public Resources(Boxes boxes) { _boxes = boxes; }

        public bool TryGet<T>(out T resource) where T : struct, IResource
        {
            if (TryBox<T>(out var box))
            {
                resource = box.Value;
                return true;
            }

            resource = default;
            return false;
        }

        public bool TryGet(Type type, out IResource resource)
        {
            if (TryBox(type, out var box))
            {
                resource = box.Value as IResource;
                return resource != null;
            }

            resource = default;
            return false;
        }

        public ref T Get<T>() where T : struct, IResource => ref Box<T>().Value;
        public IResource Get(Type type) => Box(type).Value as IResource;
        public bool Set<T>(in T resource) where T : struct, IResource => _boxes.Set(_key, resource);
        public bool Set(IResource resource) => _boxes.Set(resource.GetType(), _key, resource);

        public bool Has<T>() where T : struct, IResource => _boxes.Has<T>(_key);
        public bool Has(Type type) => _boxes.Has(type, _key);
        public bool Remove<T>() where T : struct, IResource => _boxes.Remove<T>(_key);
        public bool Remove(Type type) => _boxes.Remove(type, _key);
        public bool TryBox(Type type, out Box box) => _boxes.TryGet(type, _key, out box);
        public bool TryBox<T>(out Box<T> box) where T : struct, IResource => _boxes.TryGet<T>(_key, out box);

        public Box Box(Type type)
        {
            if (_boxes.TryGet(type, _key, out var box)) return box;
            _boxes.Set(type, _key, DefaultUtility.Default(type), out box);
            return box;
        }

        public Box<T> Box<T>() where T : struct, IResource
        {
            if (_boxes.TryGet<T>(_key, out var box)) return box;
            _boxes.Set(_key, DefaultUtility.Default<T>(), out box);
            return box;
        }

        public bool Clear() => _boxes.Clear(_key);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
        public IEnumerator<IResource> GetEnumerator() => _boxes.Get(_key)
            .Select(box => box.Value)
            .OfType<IResource>()
            .GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
