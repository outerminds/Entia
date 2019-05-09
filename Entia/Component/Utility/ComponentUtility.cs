using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core;
using Entia.Core.Documentation;

namespace Entia.Modules.Component
{
    [ThreadSafe]
    public static class ComponentUtility
    {
        public enum Kinds { Invalid, Abstract, Concrete }

        [ThreadSafe]
        public static class Concrete<T> where T : struct, IComponent
        {
            public static readonly Metadata Data = GetMetadata(typeof(T));
            public static readonly BitMask Mask = GetConcrete(typeof(T));
        }

        [ThreadSafe]
        public static class Abstract<T> where T : IComponent
        {
            public static bool IsConcrete => Kind == Kinds.Concrete;
            public static bool IsAbstract => Kind == Kinds.Abstract;

            public static readonly Kinds Kind = GetKind(typeof(T));
            public static readonly BitMask Mask = GetConcrete(typeof(T));
            public static readonly Metadata Data;

            static Abstract() { TryGetMetadata(typeof(T), out Data); }
        }

        struct State
        {
            public (Metadata[] items, int count) Concretes;
            public Dictionary<Type, Metadata> ConcreteToMetadata;
            public Dictionary<Type, BitMask> AbstractToMask;
        }

        public static int Count => _state.Read((in State state) => state.Concretes.count);

        static readonly Concurrent<State> _state = new State
        {
            Concretes = (new Metadata[8], 0),
            ConcreteToMetadata = new Dictionary<Type, Metadata>(),
            AbstractToMask = new Dictionary<Type, BitMask>()
        };

        public static bool TryGetMetadata(Type type, out Metadata data)
        {
            data = GetMetadata(type);
            return data.IsValid;
        }

        public static bool TryGetConcrete(Type type, out BitMask mask)
        {
            using (var read = _state.Read()) return read.Value.AbstractToMask.TryGetValue(type, out mask);
        }

        public static Metadata[] ToMetadata(BitMask mask)
        {
            using (var read = _state.Read())
            {
                return mask.Select(index => index < read.Value.Concretes.count ? read.Value.Concretes.items[index] : default)
                    .Where(data => data.IsValid)
                    .ToArray();
            }
        }

        public static Kinds GetKind(Type type)
        {
            if (type.Is<IComponent>())
                return type.IsValueType && type.IsSealed && !type.IsGenericTypeDefinition && !type.IsAbstract ? Kinds.Concrete : Kinds.Abstract;
            return Kinds.Invalid;
        }

        public static bool IsInvalid(Type type) => GetKind(type) == Kinds.Invalid;
        public static bool IsValid(Type type) => !IsInvalid(type);
        public static bool IsConcrete(Type type) => GetKind(type) == Kinds.Concrete;
        public static bool IsAbstract(Type type) => GetKind(type) == Kinds.Abstract;

        public static Metadata GetMetadata(Type type)
        {
            using (var read = _state.Read(true))
            {
                if (read.Value.ConcreteToMetadata.TryGetValue(type, out var data)) return data;
                return CreateMetadata(type);
            }
        }

        public static BitMask GetConcrete(Type type)
        {
            using (var read = _state.Read(true))
            {
                if (read.Value.AbstractToMask.TryGetValue(type, out var mask)) return mask;
                using (var write = _state.Write())
                {
                    if (write.Value.AbstractToMask.TryGetValue(type, out mask)) return mask;
                    return write.Value.AbstractToMask[type] = new BitMask();
                }
            }
        }

        public static BitMask[] GetMasks(params Type[] types)
        {
            var (concrete, @abstract) = types.Where(ComponentUtility.IsValid).Split(ComponentUtility.IsConcrete);
            var masks = @abstract.Select(ComponentUtility.GetConcrete);
            if (concrete.Length > 0) masks = masks.Prepend(new BitMask(concrete.Select(component => ComponentUtility.GetMetadata(component).Index).ToArray()));
            return masks.ToArray();
        }

        static Metadata CreateMetadata(Type type)
        {
            if (IsConcrete(type))
            {
                var abstracts = type.Hierarchy()
                    .SelectMany(child => child.IsGenericType ? new[] { child, child.GetGenericTypeDefinition() } : new[] { child })
                    .Where(child => child.Is<IComponent>())
                    .ToArray();
                using (var write = _state.Write())
                {
                    if (write.Value.ConcreteToMetadata.TryGetValue(type, out var data)) return data;

                    var index = write.Value.Concretes.count;
                    data = new Metadata(TypeUtility.GetData(type), index);
                    write.Value.ConcreteToMetadata[type] = data;
                    write.Value.Concretes.Push(data);
                    foreach (var @abstract in abstracts) GetConcrete(@abstract).Add(index);
                    return data;
                }
            }

            return default;
        }
    }
}