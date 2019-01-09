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
        readonly TypeMap<IResource, IBox> _boxes = new TypeMap<IResource, IBox>();

        public ref T Get<T>() where T : struct, IResource => ref Box<T>().Value;
        public IResource Get(Type resource) => (IResource)Box(resource).Value;
        public void Set<T>(in T resource) where T : struct, IResource => Box<T>().Value = resource;
        public void Set(IResource resource) => Box(resource.GetType()).Value = resource;

        public bool CopyTo(Resources resources)
        {
            foreach (var pair in _boxes) pair.value.CopyTo(resources.Box(pair.type));
            return true;
        }

        public bool Remove<T>() where T : struct, IResource
        {
            if (TryBox<T>(out var box))
            {
                box.Value = default;
                return true;
            }

            return false;
        }

        public bool Remove(Type resource)
        {
            if (TryBox(resource, out var box))
            {
                box.Value = null;
                return true;
            }

            return false;
        }

        public bool TryBox(Type resource, out IBox box) => _boxes.TryGet(resource, out box);

        public bool TryBox<T>(out Box<T> box) where T : struct, IResource
        {
            if (_boxes.TryGet<T>(out var value) && value is Box<T> casted)
            {
                box = casted;
                return true;
            }

            box = default;
            return false;
        }

        public IBox Box(Type resource)
        {
            if (TryBox(resource, out var box)) return box;
            var type = typeof(Box<>).MakeGenericType(resource);
            _boxes.Set(resource, box = Activator.CreateInstance(type) as IBox);
            return box;
        }

        public Box<T> Box<T>() where T : struct, IResource
        {
            if (TryBox<T>(out var box)) return box;
            _boxes.Set<T>(box = new Box<T>());
            return box;
        }

        public bool Clear() => _boxes.Clear();
        public IEnumerator<IResource> GetEnumerator() => _boxes.Values.Select(pair => pair.Value).OfType<IResource>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
