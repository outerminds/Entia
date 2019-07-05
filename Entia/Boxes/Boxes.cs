using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Core.Documentation;

namespace Entia.Modules
{
    [ThreadSafe]
    public sealed class Boxes : IModule, IClearable
    {
        struct Data
        {
            public Box Box;
            public Dictionary<object, Box> Map;
        }

        readonly Concurrent<TypeMap<object, Data>> _boxes = new TypeMap<object, Data>();

        public bool TryGet<T>(out Box<T> box) => TryGet<T>(null, out box);
        public bool TryGet<T>(object key, out Box<T> box)
        {
            using (var read = _boxes.Read())
            {
                ref var data = ref read.Value.Get<T>(out var success);
                if (success)
                {
                    if (key is null) return data.Box.TryAs<T>(out box);
                    return data.Map.TryGetValue(key, out var value) & value.TryAs<T>(out box);
                }

                box = default;
                return false;
            }
        }
        public bool TryGet(Type type, out Box box) => TryGet(type, null, out box);
        public bool TryGet(Type type, object key, out Box box)
        {
            using (var read = _boxes.Read())
            {
                ref var data = ref read.Value.Get(type, out var success);
                if (success)
                {
                    if (key is null) return (box = data.Box).Valid;
                    return data.Map.TryGetValue(key, out box);
                }

                box = default;
                return false;
            }
        }

        public IEnumerable<Box> Get(object key)
        {
            using (var read = _boxes.Read())
                foreach (var data in read.Value.Values)
                    if (data.Map.TryGetValue(key, out var box)) yield return box;
        }

        public bool Has<T>() => Has<T>(null);
        public bool Has<T>(object key)
        {
            using (var read = _boxes.Read())
            {
                ref var data = ref read.Value.Get<T>(out var success);
                return success && Has(data, key);
            }
        }
        public bool Has(Type type) => Has(type, null);
        public bool Has(Type type, object key)
        {
            using (var read = _boxes.Read())
            {
                ref var data = ref read.Value.Get(type, out var success);
                return success && Has(data, key);
            }
        }

        public bool Set<T>(in T value, out Box<T> box) => Set(null, value, out box);
        public bool Set<T>(in T value) => Set(null, value, out _);
        public bool Set<T>(object key, in T value) => Set(key, value, out _);
        public bool Set<T>(object key, in T value, out Box<T> box)
        {
            using (var write = _boxes.Write())
            {
                ref var data = ref write.Value.Get<T>(out var success);
                if (success)
                {
                    if (key is null)
                    {
                        if (data.Box.TryAs<T>(out box))
                        {
                            box.Value = value;
                            return false;
                        }
                        else
                        {
                            data.Box = box = new Box<T>(value);
                            return true;
                        }
                    }
                    else if (data.Map.TryGetValue(key, out var current) && current.TryAs<T>(out box))
                    {
                        box.Value = value;
                        return false;
                    }
                    else
                    {
                        data.Map[key] = box = new Box<T>(value);
                        return true;
                    }
                }

                write.Value.Set<T>(key is null ?
                    new Data { Box = new Box<T>(value), Map = new Dictionary<object, Box>() } :
                    new Data { Map = new Dictionary<object, Box> { { key, box = new Box<T>(value) } } });
                return true;
            }
        }
        public bool Set(Type type, object value, out Box box) => Set(type, null, value, out box);
        public bool Set(Type type, object value) => Set(type, null, value, out _);
        public bool Set(Type type, object key, object value) => Set(type, key, value, out _);
        public bool Set(Type type, object key, object value, out Box box)
        {
            using (var write = _boxes.Write())
            {
                ref var data = ref write.Value.Get(type, out var success);
                if (success)
                {
                    if (key is null)
                    {
                        if (data.Box.Valid)
                        {
                            box = data.Box;
                            box.Value = value;
                            return false;
                        }
                        else
                        {
                            data.Box = box = new Box(value, type);
                            return true;
                        }
                    }
                    else if (data.Map.TryGetValue(key, out box))
                    {
                        box.Value = value;
                        return false;
                    }
                    else
                    {
                        data.Map[key] = box = new Box(value, type);
                        return true;
                    }
                }

                write.Value.Set(type, key is null ?
                    new Data { Box = new Box(value, type), Map = new Dictionary<object, Box>() } :
                    new Data { Map = new Dictionary<object, Box> { { key, box = new Box(value, type) } } });
                return true;
            }
        }

        public bool Remove<T>() => Remove<T>(null);
        public bool Remove<T>(object key)
        {
            using (var write = _boxes.Write())
            {
                ref var data = ref write.Value.Get<T>(out var success);
                return success && Remove(ref data, key);
            }
        }
        public bool Remove(Type type) => Remove(type, null);
        public bool Remove(Type type, object key)
        {
            using (var write = _boxes.Write())
            {
                ref var data = ref write.Value.Get(type, out var success);
                return success && Remove(ref data, key);
            }
        }

        public bool Clear(object key)
        {
            var cleared = false;
            using (var write = _boxes.Write())
                foreach (var data in write.Value.Values)
                    cleared |= data.Map.Remove(key);
            return cleared;
        }

        public bool Clear() => _boxes.Write(boxes => boxes.Clear());

        bool Has(in Data data, object key)
        {
            if (key is null) return data.Box.Valid;
            return data.Map.ContainsKey(key);
        }

        bool Remove(ref Data data, object key)
        {
            if (key is null)
            {
                if (data.Box.Valid)
                {
                    data.Box = default;
                    return true;
                }
                return false;
            }
            return data.Map.Remove(key);
        }
    }
}