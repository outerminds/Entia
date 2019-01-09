using System;
using System.Collections.Generic;
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

        public static int Count => _metadata.ReadCount();

        static readonly Concurrent<(Metadata[] items, int count)> _metadata = (new Metadata[8], 0);
        static readonly Concurrent<Dictionary<Type, Metadata>> _typeToMetadata = new Dictionary<Type, Metadata>();

        public static bool TryGetMetadata(int index, out Metadata data) => _metadata.TryReadAt(index, out data);

        public static bool TryGetMetadata(Type type, out Metadata data)
        {
            data = _typeToMetadata.ReadValueOrWrite(type, type, key => (key, CreateMetadata(key)));
            return data.IsValid;
        }

        static Metadata CreateMetadata(Type type)
        {
            if (type.Is<IComponent>())
            {
                using (var metadata = _metadata.Write())
                {
                    var data = new Metadata(type, metadata.Value.count, new BitMask(metadata.Value.count), type.GetFields(TypeUtility.Instance));
                    return metadata.Value.Push(data);
                }
            }

            return new Metadata(type, -1, new BitMask(), Array.Empty<FieldInfo>());
        }
    }
}