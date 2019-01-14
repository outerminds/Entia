using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core;

namespace Entia.Modules.Component
{
    public static class ComponentUtility
    {
        public static class Cache<T> where T : IComponent
        {
            public static readonly Metadata Data;
            static Cache() { TryGetMetadata(typeof(T), out Data); }
        }

        struct State
        {
            public (Metadata[] items, int count) Metadata;
            public Dictionary<Type, Metadata> TypeToMetadata;
        }

        public static int Count => _state.Read((in State state) => state.Metadata.count);
        public static Metadata[] Types => _state.Read((in State state) => state.Metadata.ToArray());

        static readonly Concurrent<State> _state = new State
        {
            Metadata = (new Metadata[8], 0),
            TypeToMetadata = new Dictionary<Type, Metadata>()
        };

        public static bool TryGetMetadata(int index, out Metadata data)
        {
            using (var read = _state.Read())
            {
                if (index < read.Value.Metadata.count)
                {
                    data = read.Value.Metadata.items[index];
                    return data.IsValid;
                }
            }

            data = default;
            return true;
        }

        public static bool TryGetMetadata(Type type, out Metadata data)
        {
            using (var read = _state.Read(true))
            {
                if (read.Value.TypeToMetadata.TryGetValue(type, out data)) return data.IsValid;
                using (var write = _state.Write())
                {
                    if (write.Value.TypeToMetadata.TryGetValue(type, out data)) return data.IsValid;
                    data = write.Value.TypeToMetadata[type] = CreateMetadata(type);
                }
            }

            return data.IsValid;
        }

        public static bool ToMetadataAndMask(Type[] components, out Metadata[] types, out BitMask mask)
        {
            types = components
                .Select(component => ComponentUtility.TryGetMetadata(component, out var metadata) ? metadata : default)
                .Where(metadata => metadata.IsValid)
                .ToArray();
            mask = new BitMask(types.Select(metadata => metadata.Index).ToArray());
            return components.Length == types.Length;
        }

        static Metadata CreateMetadata(Type type)
        {
            if (type.Is<IComponent>())
            {
                using (var write = _state.Write())
                {
                    var data = new Metadata(
                        type,
                        write.Value.Metadata.count,
                        new BitMask(write.Value.Metadata.count),
                        type.GetFields(TypeUtility.Instance));
                    return write.Value.Metadata.Push(data);
                }
            }

            return Metadata.Invalid;
        }
    }
}