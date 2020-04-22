using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Experimental.Serializers;

namespace Entia.Modules
{
    [ThreadSafe]
    public sealed class Boxes : IModule, IEnumerable<(Type type, object key, object value)>
    {
        struct Data
        {
            [Implementation]
            static Serializer<Data> _serializer => Serializer.Map(
                (in Data data) => (data.Box, data.Map),
                (in (Box box, Dictionary<object, Box> map) pair) => new Data { Box = pair.box, Map = pair.map },
                Serializer.Tuple<Box, Dictionary<object, Box>>(default, Serializer.Dictionary<object, Box>()));

            public Box Box;
            public Dictionary<object, Box> Map;
        }

        [Implementation]
        static Serializer<Boxes> _serializer => Serializer.Object(
            () => new Boxes(),
            Serializer.Member.Property(
                (in Boxes boxes) => boxes._boxes.Read(value => (value.Keys.ToArray(), value.Values.ToArray())),
                (ref Boxes boxes, in (Type[], Data[]) pair) =>
                {
                    var (keys, values) = pair;
                    using var write = boxes._boxes.Write();
                    for (int i = 0; i < keys.Length; i++) write.Value.Set(keys[i], values[i]);
                })
        );

        readonly Concurrent<TypeMap<object, Data>> _boxes = new TypeMap<object, Data>();

        public bool TryGet<T>(out Box<T> box) => TryGet<T>(null, out box);
        public bool TryGet<T>(object key, out Box<T> box)
        {
            using var read = _boxes.Read();
            ref var data = ref read.Value.Get<T>(out var success);
            if (success)
            {
                if (key is null) return data.Box.TryAs<T>(out box);
                return data.Map.TryGetValue(key, out var value) & value.TryAs<T>(out box);
            }

            box = default;
            return false;
        }
        public bool TryGet(Type type, out Box box) => TryGet(type, null, out box);
        public bool TryGet(Type type, object key, out Box box)
        {
            using var read = _boxes.Read();
            ref var data = ref read.Value.Get(type, out var success);
            if (success)
            {
                if (key is null) return (box = data.Box).IsValid;
                return data.Map.TryGetValue(key, out box);
            }

            box = default;
            return false;
        }

        public IEnumerable<Box> Get(object key)
        {
            using var read = _boxes.Read();
            foreach (var data in read.Value.Values)
                if (data.Map.TryGetValue(key, out var box)) yield return box;
        }

        public bool Has<T>() => Has<T>(null);
        public bool Has<T>(object key)
        {
            using var read = _boxes.Read();
            ref var data = ref read.Value.Get<T>(out var success);
            return success && Has(data, key);
        }
        public bool Has(Type type) => Has(type, null);
        public bool Has(Type type, object key)
        {
            using var read = _boxes.Read();
            ref var data = ref read.Value.Get(type, out var success);
            return success && Has(data, key);
        }

        public bool Set<T>(in T value, out Box<T> box, bool overwrite = true) => Set(null, value, out box, overwrite);
        public bool Set<T>(in T value, bool overwrite = true) => Set(null, value, out _, overwrite);
        public bool Set<T>(object key, in T value, bool overwrite = true) => Set(key, value, out _, overwrite);
        public bool Set<T>(object key, in T value, out Box<T> box, bool overwrite = true)
        {
            using var write = _boxes.Write();
            ref var data = ref write.Value.Get<T>(out var success);
            if (success)
            {
                if (key is null)
                {
                    if (data.Box.TryAs(out box))
                    {
                        if (overwrite) box.Value = value;
                        return false;
                    }
                    else
                    {
                        data.Box = box = new Box<T>(value);
                        return true;
                    }
                }
                else if (data.Map.TryGetValue(key, out var current) && current.TryAs(out box))
                {
                    if (overwrite) box.Value = value;
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
        public bool Set(Type type, object value, out Box box, bool overwrite = true) => Set(type, null, value, out box, overwrite);
        public bool Set(Type type, object value, bool overwrite = true) => Set(type, null, value, out _, overwrite);
        public bool Set(Type type, object key, object value, bool overwrite = true) => Set(type, key, value, out _, overwrite);
        public bool Set(Type type, object key, object value, out Box box, bool overwrite = true)
        {
            using var write = _boxes.Write();
            ref var data = ref write.Value.Get(type, out var success);
            if (success)
            {
                if (key is null)
                {
                    if (data.Box.IsValid)
                    {
                        box = data.Box;
                        if (overwrite) box.Value = value;
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
                    if (overwrite) box.Value = value;
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

        public bool Remove<T>() => Remove<T>(null, out _);
        public bool Remove<T>(out T value) => Remove(null, out value);
        public bool Remove<T>(object key) => Remove<T>(key, out _);
        public bool Remove<T>(object key, out T value)
        {
            using var write = _boxes.Write();
            ref var data = ref write.Value.Get<T>(out var success);
            if (success && Remove(ref data, key, out var box) && box.TryAs<T>(out var casted))
            {
                value = casted.Value;
                return true;
            }
            value = default;
            return false;
        }
        public bool Remove(Type type) => Remove(type, null, out _);
        public bool Remove(Type type, out object value) => Remove(type, null, out value);
        public bool Remove(Type type, object key) => Remove(type, key, out _);
        public bool Remove(Type type, object key, out object value)
        {
            using var write = _boxes.Write();
            ref var data = ref write.Value.Get(type, out var success);
            if (success && Remove(ref data, key, out var box))
            {
                value = box.Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool Clear(object key)
        {
            var cleared = false;
            using var write = _boxes.Write();
            foreach (var data in write.Value.Values)
                cleared |= data.Map.Remove(key);
            return cleared;
        }

        public bool Clear() => _boxes.Write(boxes => boxes.Clear());

        bool Has(in Data data, object key)
        {
            if (key is null) return data.Box.IsValid;
            return data.Map.ContainsKey(key);
        }

        bool Remove(ref Data data, object key, out Box box)
        {
            if (key is null)
            {
                box = data.Box;
                if (data.Box.IsValid)
                {
                    data.Box = default;
                    return true;
                }
                return false;
            }
            else
                return data.Map.TryGetValue(key, out box) && box.IsValid && data.Map.Remove(key);
        }

        public IEnumerator<(Type type, object key, object value)> GetEnumerator() =>
            _boxes.Read(boxes => boxes
                .SelectMany(pair => pair.value.Map
                    .Select(pair2 => (pair.type, key: pair2.Key, box: pair2.Value))
                    .Prepend((pair.type, null, pair.value.Box))
                    .Where(data => data.box.IsValid)
                    .Select(data => (data.type, data.key, data.box.Value)))
                .ToArray())
            .Slice()
            .GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}