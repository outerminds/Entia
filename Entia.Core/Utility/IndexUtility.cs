using System;
using System.Collections.Generic;

namespace Entia.Core
{
    public static class IndexUtility
    {
        public static int Count => _types.ReadCount();
        public static Type[] Types => _types.ReadToArray();

        static readonly Concurrent<List<Type>> _types = new List<Type>();
        static readonly Concurrent<Dictionary<Type, int>> _typeToIndex = new Dictionary<Type, int>();
        static readonly Concurrent<Dictionary<Type, BitMask>> _typeToMask = new Dictionary<Type, BitMask>();
        static readonly Concurrent<Dictionary<BitMask, Type[]>> _maskToTypes = new Dictionary<BitMask, Type[]>();

        public static Type[] GetMaskTypes(BitMask mask) =>
            _maskToTypes.ReadValueOrWrite(mask, mask, key =>
            {
                var list = new List<Type>();
                foreach (var index in key) if (TryGetType(index, out var type)) list.Add(type);
                return (new BitMask(key), list.ToArray());
            });

        public static BitMask GetMask(Type type) => _typeToMask.ReadValueOrWrite(type, type, key => (key, new BitMask(GetIndex(key))));

        public static bool TryGetType(int index, out Type type) => _types.TryReadAt(index, out type);
        public static bool TryGetIndex(Type type, out int index) => _typeToIndex.TryReadValue(type, out index);
        public static int GetIndex(Type type) =>
            _typeToIndex.ReadValueOrWrite(type, type, key =>
            {
                using (var types = _types.Write())
                {
                    var index = types.Value.Count;
                    types.Value.Add(key);
                    return (key, index);
                }
            });
    }

    public static class IndexUtility<TBase>
    {
        public static class Cache<T> where T : TBase
        {
            public static readonly (int global, int local) Index = TryGetIndex(typeof(T), out var index) ? index : (-1, -1);
            public static readonly BitMask Mask = IndexUtility.GetMask(typeof(T));
        }

        public static int Count => _types.ReadCount();
        public static (int global, int local, Type type)[] Types => _types.ReadToArray();

        static readonly Concurrent<List<(int global, int local, Type type)>> _types = new List<(int global, int local, Type type)>();
        static readonly Concurrent<Dictionary<Type, (int global, int local)>> _indices = new Dictionary<Type, (int global, int local)>();
        static readonly Concurrent<Dictionary<BitMask, (int global, int local, Type type)[]>> _maskToTypes = new Dictionary<BitMask, (int global, int local, Type type)[]>();

        public static (int global, int local, Type type)[] GetMaskTypes(BitMask mask) =>
            _maskToTypes.ReadValueOrWrite(mask, mask, key =>
            {
                var list = new List<(int global, int local, Type type)>();
                foreach (var global in key)
                {
                    if (IndexUtility.TryGetType(global, out var type) && TryGetIndex(type, out var index))
                        list.Add((index.global, index.local, type));
                }
                return (new BitMask(key), list.ToArray());
            });

        public static bool TryGetType(int local, out Type type)
        {
            if (_types.TryReadAt(local, out var index))
            {
                type = index.type;
                return true;
            }

            type = default;
            return false;
        }

        public static bool TryGetIndex(Type type, out (int global, int local) index)
        {
            index = _indices.ReadValueOrWrite(type, type, key =>
            {
                using (var types = _types.Write())
                {
                    if (Is(key))
                    {
                        var global = IndexUtility.GetIndex(key);
                        var local = types.Value.Count;
                        types.Value.Add((global, local, key));
                        return (key, (global, local));
                    }

                    return (key, (-1, -1));
                }
            });

            return index.local >= 0;
        }

        public static bool Is(Type type) => type.Is<TBase>();
    }
}