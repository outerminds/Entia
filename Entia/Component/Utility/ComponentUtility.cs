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
        public static class Cache<T> where T : struct, IComponent
        {
            [Preserve]
            public static readonly Metadata Data = GetMetadata(typeof(T));
            [Preserve]
            public static readonly Pointer<T> Pointer = new Pointer<T>();
        }

        struct State
        {
            public (Metadata[] items, int count) Concretes;
            public TypeMap<IComponent, Metadata> ConcreteToMetadata;
            public TypeMap<IComponent, BitMask> AbstractToMask;
            public TypeMap<IComponent, Metadata[]> AbstractToMetadata;
            public Dictionary<BitMask, Metadata[]> MaskToMetadata;
        }

        public static int Count => _state.Read((in State state) => state.Concretes.count);

        static readonly Concurrent<State> _state = new State
        {
            Concretes = (new Metadata[8], 0),
            ConcreteToMetadata = new TypeMap<IComponent, Metadata>(),
            AbstractToMask = new TypeMap<IComponent, BitMask>(),
            AbstractToMetadata = new TypeMap<IComponent, Metadata[]>(),
            MaskToMetadata = new Dictionary<BitMask, Metadata[]>()
        };

        public static bool TryGetMetadata<T>(bool create, out Metadata data) where T : IComponent
        {
            using (var read = _state.Read(create))
            {
                if (read.Value.ConcreteToMetadata.TryGet<T>(out data)) return data.IsValid;
                if (create) data = CreateMetadata(typeof(T));
                return data.IsValid;
            }
        }

        public static bool TryGetMetadata(Type type, bool create, out Metadata data)
        {
            using (var read = _state.Read(create))
            {
                if (read.Value.ConcreteToMetadata.TryGet(type, out data)) return data.IsValid;
                else if (create) data = CreateMetadata(type);
                return data.IsValid;
            }
        }

        public static bool TryGetConcreteMask<T>(out BitMask mask) where T : IComponent
        {
            using (var read = _state.Read()) return read.Value.AbstractToMask.TryGet<T>(out mask);
        }

        public static bool TryGetConcreteTypes<T>(out Metadata[] types) where T : IComponent
        {
            using (var read = _state.Read()) return read.Value.AbstractToMetadata.TryGet<T>(out types);
        }

        public static bool TryGetConcrete<T>(out BitMask mask, out Metadata[] types) where T : IComponent
        {
            using (var read = _state.Read())
                return read.Value.AbstractToMask.TryGet<T>(out mask) & read.Value.AbstractToMetadata.TryGet<T>(out types);
        }

        public static bool TryGetConcreteMask(Type type, out BitMask mask)
        {
            using (var read = _state.Read()) return read.Value.AbstractToMask.TryGet(type, out mask);
        }

        public static bool TryGetConcreteTypes(Type type, out Metadata[] types)
        {
            using (var read = _state.Read()) return read.Value.AbstractToMetadata.TryGet(type, out types);
        }

        public static bool TryGetConcrete(Type type, out BitMask mask, out Metadata[] types)
        {
            using (var read = _state.Read())
                return read.Value.AbstractToMask.TryGet(type, out mask) & read.Value.AbstractToMetadata.TryGet(type, out types);
        }

        public static Metadata[] GetConcreteTypes(BitMask mask)
        {
            using (var read = _state.Read(true))
                return read.Value.MaskToMetadata.TryGetValue(mask, out var types) ? types : CreateConcreteTypes(mask);
        }

        public static Metadata GetMetadata<T>() where T : struct, IComponent
        {
            using (var read = _state.Read(true))
            {
                if (read.Value.ConcreteToMetadata.TryGet<T>(out var data)) return data;
                return CreateMetadata(typeof(T));
            }
        }

        public static Metadata GetMetadata(Type type)
        {
            using (var read = _state.Read(true))
            {
                if (read.Value.ConcreteToMetadata.TryGet(type, out var data)) return data;
                return CreateMetadata(type);
            }
        }

        public static bool TryGetMetadata(int index, out Metadata data)
        {
            using (var read = _state.Read()) return read.Value.Concretes.TryGet(index, out data) && data.IsValid;
        }

        public static BitMask GetConcreteMask(Type type)
        {
            using (var read = _state.Read(true))
            {
                if (read.Value.AbstractToMask.TryGet(type, out var mask)) return mask;
                using (var write = _state.Write())
                {
                    return
                        write.Value.AbstractToMask.TryGet(type, out mask) ? mask :
                        write.Value.AbstractToMask[type] = new BitMask();
                }
            }
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

        public static bool TryAsPointer(this Type type, out Type pointer)
        {
            if (type.IsPointer && type.GetElementType() is Type element && IsConcrete(element))
            {
                pointer = typeof(Pointer<>).MakeGenericType(element);
                return true;
            }

            pointer = default;
            return false;
        }

        public static BitMask[] GetMasks(params Type[] types)
        {
            var (concrete, @abstract) = types.Where(ComponentUtility.IsValid).Split(ComponentUtility.IsConcrete);
            var masks = @abstract.Select(type => ComponentUtility.GetConcreteMask(type));
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
                    if (write.Value.ConcreteToMetadata.TryGet(type, out var data)) return data;

                    var index = write.Value.Concretes.count;
                    data = new Metadata(TypeUtility.GetData(type), index);
                    write.Value.ConcreteToMetadata[type] = data;
                    write.Value.Concretes.Push(data);
                    foreach (var @abstract in abstracts)
                    {
                        if (write.Value.AbstractToMask.TryGet(@abstract, out var mask)) mask.Add(index);
                        else write.Value.AbstractToMask[@abstract] = new BitMask(index);

                        ref var types = ref write.Value.AbstractToMetadata.Get(@abstract, out var success);
                        if (success) ArrayUtility.Add(ref types, data);
                        else write.Value.AbstractToMetadata[@abstract] = new[] { data };
                    }
                    return data;
                }
            }

            return default;
        }

        static Metadata[] CreateConcreteTypes(BitMask mask)
        {
            var list = new List<Metadata>(mask.Capacity);
            foreach (var index in mask) if (TryGetMetadata(index, out var metadata)) list.Add(metadata);

            using (var write = _state.Write())
            {
                return
                    write.Value.MaskToMetadata.TryGetValue(mask, out var types) ? types :
                    write.Value.MaskToMetadata[new BitMask(mask)] = list.ToArray();
            }
        }
    }
}