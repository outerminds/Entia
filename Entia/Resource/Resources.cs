using Entia.Core;
using Entia.Injectables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed class Resources : IModule, IEnumerable<IResource>
    {
        public TypeMap<IResource, IBox>.ValueEnumerable Boxes => _boxes.Values;

        readonly TypeMap<IResource, IBox> _boxes = new TypeMap<IResource, IBox>();

        public ref T Get<T>() where T : struct, IResource => ref GetBox<T>().Value;
        public IResource Get(Type resource) => (IResource)GetBox(resource).Value;
        public void Set<T>(in T resource) where T : struct, IResource => GetBox<T>().Value = resource;
        public void Set(IResource resource) => GetBox(resource.GetType()).Value = resource;

        public bool Remove<T>() where T : struct, IResource
        {
            if (TryGetBox<T>(out var box))
            {
                box.Value = default;
                return true;
            }

            return false;
        }

        public bool Remove(Type resource)
        {
            if (TryGetBox(resource, out var box))
            {
                box.Value = null;
                return true;
            }

            return false;
        }

        public bool TryGetBox(Type resource, out IBox box) => _boxes.TryGet(resource, out box);

        public bool TryGetBox<T>(out Box<T> box) where T : struct, IResource
        {
            if (_boxes.TryGet<T>(out var value) && value is Box<T> casted)
            {
                box = casted;
                return true;
            }

            box = default;
            return false;
        }

        public IBox GetBox(Type resource)
        {
            if (TryGetBox(resource, out var box)) return box;
            var type = typeof(Box<>).MakeGenericType(resource);
            _boxes.Set(resource, box = Activator.CreateInstance(type) as IBox);
            return box;
        }

        public Box<T> GetBox<T>() where T : struct, IResource
        {
            if (TryGetBox<T>(out var box)) return box;
            _boxes.Set<T>(box = new Box<T>());
            return box;
        }

        public bool Clear() => _boxes.Clear();
        public IEnumerator<IResource> GetEnumerator() => _boxes.Values.Select(pair => pair.Value).OfType<IResource>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
