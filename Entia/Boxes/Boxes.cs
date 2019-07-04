using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Modules
{
    public sealed class Boxes : IModule, IClearable
    {
        readonly Dictionary<(Type, object), IBox> _boxes = new Dictionary<(Type, object), IBox>();

        public bool TryGet<T>(object key, out Box<T> box)
        {
            if (TryGet(typeof(T), key, out var value) && value is Box<T> casted)
            {
                box = casted;
                return true;
            }

            box = default;
            return false;
        }

        public bool TryGet(Type type, object key, out IBox box) => _boxes.TryGetValue((type, key), out box);

        public bool Has<T>(object key) => _boxes.ContainsKey((typeof(T), key));
        public bool Has(Type type, object key) => _boxes.ContainsKey((type, key));

        public bool Set<T>(object key, in T value, out Box<T> box)
        {
            if (TryGet<T>(key, out box))
            {
                box.Value = value;
                return false;
            }

            _boxes[(typeof(T), key)] = box = new Box<T>(value);
            return true;
        }

        public bool Set<T>(object key, in T value) => Set(key, value, out _);

        public bool Remove<T>(object key) => _boxes.Remove((typeof(T), key));
        public bool Remove(Type type, object key) => _boxes.Remove((type, key));
        public bool Clear() => _boxes.TryClear();
    }
}