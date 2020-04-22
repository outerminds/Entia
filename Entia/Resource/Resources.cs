using Entia.Core;
using Entia.Core.Documentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    [ThreadSafe]
    public sealed class Resources : IModule, IEnumerable<IResource>
    {
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
        public bool Set<T>(in T resource) where T : struct, IResource => _boxes.Set(this, resource);
        public bool Set(IResource resource) => _boxes.Set(resource.GetType(), this, resource);

        public bool Has<T>() where T : struct, IResource => _boxes.Has<T>(this);
        public bool Has(Type type) => _boxes.Has(type, this);
        public bool Remove<T>() where T : struct, IResource => _boxes.Remove<T>(this);
        public bool Remove(Type type) => _boxes.Remove(type, this);
        public bool TryBox(Type type, out Box box) => _boxes.TryGet(type, this, out box);
        public bool TryBox<T>(out Box<T> box) where T : struct, IResource => _boxes.TryGet(this, out box);

        public Box Box(Type type)
        {
            if (TryBox(type, out var box)) return box;
            _boxes.Set(type, this, DefaultUtility.Default(type), out box);
            return box;
        }

        public Box<T> Box<T>() where T : struct, IResource
        {
            if (TryBox<T>(out var box)) return box;
            _boxes.Set(this, DefaultUtility.Default<T>(), out box);
            return box;
        }

        public bool Clear() => _boxes.Clear(this);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
        public IEnumerator<IResource> GetEnumerator() => _boxes.Get(this)
            .Select(box => box.Value)
            .OfType<IResource>()
            .GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
