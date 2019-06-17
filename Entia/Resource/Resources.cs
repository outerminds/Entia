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
        readonly Concurrent<TypeMap<IResource, Array>> _boxes = new TypeMap<IResource, Array>();

        public bool TryGet<T>(out T resource) where T : struct, IResource
        {
            if (TryBox<T>(out var box))
            {
                resource = box[0];
                return true;
            }

            resource = default;
            return false;
        }

        public bool TryGet(Type type, out IResource resource)
        {
            if (TryBox(type, out var box))
            {
                resource = box.GetValue(0) as IResource;
                return resource != null;
            }

            resource = default;
            return false;
        }

        public ref T Get<T>() where T : struct, IResource => ref Box<T>()[0];
        public IResource Get(Type type) => Box(type).GetValue(0) as IResource;
        public void Set<T>(in T resource) where T : struct, IResource => Box<T>()[0] = resource;
        public void Set(IResource resource) => Box(resource.GetType()).SetValue(resource, 0);

        public bool Has<T>() where T : struct, IResource
        {
            using (var read = _boxes.Read()) return read.Value.Has<T>(false, false);
        }

        public bool Has(Type type)
        {
            using (var read = _boxes.Read()) return read.Value.Has(type, false, false);
        }

        public bool Remove<T>() where T : struct, IResource
        {
            using (var write = _boxes.Write()) return write.Value.Remove<T>(false, false);
        }

        public bool Remove(Type type)
        {
            using (var write = _boxes.Write()) return write.Value.Remove(type, false, false);
        }

        public bool TryBox(Type type, out Array box)
        {
            using (var read = _boxes.Read()) return read.Value.TryGet(type, out box, false, false);
        }

        public bool TryBox<T>(out T[] box) where T : struct, IResource
        {
            using (var read = _boxes.Read())
            {
                if (read.Value.TryGet<T>(out var value, false, false) && value is T[] casted)
                {
                    box = casted;
                    return true;
                }

                box = default;
                return false;
            }
        }

        public Array Box(Type type)
        {
            using (var read = _boxes.Read(true))
            {
                if (read.Value.TryGet(type, out var box, false, false)) return box;
                using (var write = _boxes.Write())
                {
                    if (write.Value.TryGet(type, out box, false, false)) return box;
                    write.Value.Set(type, box = Array.CreateInstance(type, 1));
                    box.SetValue(DefaultUtility.Default(type), 0);
                    return box;
                }
            }
        }

        public T[] Box<T>() where T : struct, IResource
        {
            using (var read = _boxes.Read(true))
            {
                if (read.Value.TryGet<T>(out var box, false, false) && box is T[] casted1) return casted1;
                using (var write = _boxes.Write())
                {
                    if (write.Value.TryGet<T>(out box, false, false) && box is T[] casted2) return casted2;
                    write.Value.Set<T>(casted2 = new T[] { DefaultUtility.Default<T>() });
                    return casted2;
                }
            }
        }

        public bool Clear()
        {
            using (var write = _boxes.Write()) return write.Value.Clear();
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
        public IEnumerator<IResource> GetEnumerator() => _boxes.Read(boxes => boxes.Values.ToArray())
            .Select(pair => pair.GetValue(0))
            .OfType<IResource>()
            .GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
