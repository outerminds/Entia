using System;
using System.Collections.Generic;
using System.Reflection;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Serialization;

namespace Entia.Experiment
{
    public sealed class Descriptors : IModule
    {
        enum Kinds : byte { None, Null, Abstract, Concrete, Reference }

        static class Cache<T>
        {
            public static readonly InFunc<T, bool> IsNull = typeof(T).IsValueType ?
                new InFunc<T, bool>((in T _) => false) :
                new InFunc<T, bool>((in T value) => value == null);
        }

        readonly struct Members
        {
            [Preserve]
            public readonly object Field;
            [Preserve]
            public object Property { get; }
            [Preserve]
            public void Method() { }

            public Members(object field, object property) { Field = field; Property = property; }
        }

        static readonly object[] _references = {
            typeof(bool), typeof(bool[]), typeof(bool*), typeof(bool?),
            typeof(char), typeof(char[]), typeof(char*), typeof(char?),
            typeof(byte), typeof(byte[]), typeof(byte*), typeof(byte?),
            typeof(sbyte), typeof(sbyte[]), typeof(sbyte*), typeof(sbyte?),
            typeof(short), typeof(short[]), typeof(short*), typeof(short?),
            typeof(ushort), typeof(ushort[]), typeof(ushort*), typeof(ushort?),
            typeof(int), typeof(int[]), typeof(int*), typeof(int?),
            typeof(uint), typeof(uint[]), typeof(uint*), typeof(uint?),
            typeof(long), typeof(long[]), typeof(long*), typeof(long?),
            typeof(ulong), typeof(ulong[]), typeof(ulong*), typeof(ulong?),
            typeof(float), typeof(float[]), typeof(float*), typeof(float?),
            typeof(double), typeof(double[]), typeof(double*), typeof(double?),
            typeof(decimal), typeof(decimal[]), typeof(decimal*), typeof(decimal?),
            typeof(IntPtr), typeof(IntPtr[]), typeof(IntPtr*), typeof(IntPtr?),
            typeof(DateTime), typeof(DateTime[]), typeof(DateTime*), typeof(DateTime?),
            typeof(TimeSpan), typeof(TimeSpan[]), typeof(TimeSpan*), typeof(TimeSpan?),
            typeof(string), typeof(string[]),
            typeof(object), typeof(object[]),
            typeof(object).Module.GetType(), typeof(object).Assembly.GetType(),
            typeof(Members).GetType(),
            typeof(Members).GetField(nameof(Members.Field)).GetType(),
            typeof(Members).GetProperty(nameof(Members.Property)).GetType(),
            typeof(Members).GetMethod(nameof(Members.Method)).GetType(),
            typeof(Action), typeof(Pointer),
            typeof(World), typeof(Entity),
            typeof(BitMask), typeof(Unit), typeof(Disposable)
        };

        readonly World _world;
        readonly TypeMap<object, ISerializer> _serializers = new TypeMap<object, ISerializer>();
        readonly TypeMap<object, IDescriptor> _descriptors = new TypeMap<object, IDescriptor>();
        readonly TypeMap<object, IDescriptor> _defaults = new TypeMap<object, IDescriptor>();

        public Descriptors(World world) { _world = world; }

        public ISerializer Describe<T>() =>
            _serializers.TryGet<T>(out var serializer) ? serializer :
            _serializers[typeof(T)] = Get<T>().Describe(typeof(T), _world);

        public ISerializer Describe(Type type) =>
            _serializers.TryGet(type, out var serializer) ? serializer :
            _serializers[type] = Get(type).Describe(type, _world);

        public bool Serialize<T>(in T instance, out byte[] bytes, params object[] references)
        {
            using (var writer = new Writer())
            {
                var count = writer.Reserve<int>();
                var context = Context(writer, references);
                if (Serialize(instance, context))
                {
                    count.Value = context.References.Count;
                    bytes = writer.ToArray();
                    return true;
                }
                bytes = default;
                return false;
            }
        }
        public bool Serialize(object instance, Type type, out byte[] bytes, params object[] references)
        {
            using (var writer = new Writer())
            {
                var count = writer.Reserve<int>();
                var context = Context(writer, references);
                if (Serialize(instance, type, context))
                {
                    count.Value = context.References.Count;
                    bytes = writer.ToArray();
                    return true;
                }
                bytes = default;
                return false;
            }
        }
        public bool Serialize(object instance, Type type, in SerializeContext context)
        {
            if (instance is null)
            {
                context.Writer.Write(Kinds.Null);
                return true;
            }
            else if (context.References.TryGetValue(instance, out var index))
            {
                context.Writer.Write(Kinds.Reference);
                context.Writer.Write(index);
                return true;
            }

            var dynamic = instance.GetType();
            var reference = context.References[instance] = context.References.Count;
            if (type == dynamic)
            {
                context.Writer.Write(Kinds.Concrete);
                context.Writer.Write(reference);
                return Describe(dynamic).Serialize(instance, context);
            }
            else
            {
                context.Writer.Write(Kinds.Abstract);
                context.Writer.Write(reference);
                return Serialize(dynamic, dynamic.GetType(), context) && Describe(dynamic).Serialize(instance, context);
            }
        }

        public bool Serialize<T>(in T instance, in SerializeContext context)
        {
            if (Cache<T>.IsNull(instance))
            {
                context.Writer.Write(Kinds.Null);
                return true;
            }
            else if (context.References.TryGetValue(instance, out var index))
            {
                context.Writer.Write(Kinds.Reference);
                context.Writer.Write(index);
                return true;
            }

            var dynamic = instance.GetType();
            var reference = context.References[instance] = context.References.Count;
            if (typeof(T) == dynamic)
            {
                context.Writer.Write(Kinds.Concrete);
                context.Writer.Write(reference);
                return Describe<T>().Serialize<T>(instance, context);
            }
            else
            {
                context.Writer.Write(Kinds.Abstract);
                context.Writer.Write(reference);
                return Serialize(dynamic, dynamic.GetType(), context) && Describe(dynamic).Serialize<T>(instance, context);
            }
        }

        public bool Deserialize<T>(byte[] bytes, out T instance, params object[] references)
        {
            using (var reader = new Reader(bytes))
            {
                reader.Read(out int count);
                return Deserialize(out instance, Context(reader, count, references));
            }
        }

        public bool Deserialize(byte[] bytes, out object instance, Type type, params object[] references)
        {
            using (var reader = new Reader(bytes))
            {
                reader.Read(out int count);
                return Deserialize(out instance, type, Context(reader, count, references));
            }
        }

        public bool Deserialize(out object instance, Type type, in DeserializeContext context)
        {
            if (context.Reader.Read(out Kinds kind))
            {
                switch (kind)
                {
                    case Kinds.Null: instance = default; return true;
                    case Kinds.Reference:
                        if (context.Reader.Read(out int index))
                        {
                            instance = context.References[index];
                            return true;
                        }
                        break;
                    case Kinds.Abstract:
                        {
                            if (context.Reader.Read(out int reference) && Deserialize(out Type dynamic, context))
                                return Deserialize(out instance, dynamic, reference, context);
                            break;
                        }
                    case Kinds.Concrete:
                        {
                            if (context.Reader.Read(out int reference))
                                return Deserialize(out instance, type, reference, context);
                            break;
                        }
                }
            }
            instance = default;
            return false;
        }

        public bool Deserialize<T>(out T instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out Kinds kind))
            {
                switch (kind)
                {
                    case Kinds.Null: instance = default; return true;
                    case Kinds.Reference:
                        if (context.Reader.Read(out int index))
                        {
                            instance = (T)context.References[index];
                            return true;
                        }
                        break;
                    case Kinds.Abstract:
                        {
                            if (context.Reader.Read(out int reference) &&
                                Deserialize(out Type dynamic, context) &&
                                Deserialize(out var value, dynamic, reference, context))
                            {
                                instance = (T)value;
                                return true;
                            }
                            break;
                        }
                    case Kinds.Concrete:
                        {
                            if (context.Reader.Read(out int reference))
                                return Deserialize(out instance, reference, context);
                            break;
                        }
                }
            }
            instance = default;
            return false;
        }

        public IDescriptor Default<T>() => _defaults.TryGet<T>(out var descriptor) ? descriptor : Default(typeof(T));
        public IDescriptor Default(Type type) => _defaults.Default(type, typeof(IDescribable<>), typeof(DescriptorAttribute), _ => new Default());
        public bool Has<T>() => _descriptors.Has<T>(true, false);
        public bool Has(Type type) => _descriptors.Has(type, true, false);
        public IDescriptor Get<T>() => _descriptors.TryGet<T>(out var descriptor, true, false) ? descriptor : Default<T>();
        public IDescriptor Get(Type type) => _descriptors.TryGet(type, out var descriptor, true, false) ? descriptor : Default(type);
        public bool Set<T>(Descriptor<T> descriptor) => _descriptors.Set<T>(descriptor);
        public bool Set(Type type, IDescriptor descriptor) => _descriptors.Set(type, descriptor);
        public bool Set<T>(Serializer<T> serializer) => _serializers.Set<T>(serializer);
        public bool Set(Type type, ISerializer serializer) => _serializers.Set(type, serializer);
        public bool Remove<T>() => _descriptors.Remove<T>() | _serializers.Remove<T>();
        public bool Remove(Type type) => _descriptors.Remove(type) | _serializers.Remove(type);
        public bool Clear() => _defaults.Clear() | _descriptors.Clear() | _serializers.Clear();

        SerializeContext Context(Writer writer, params object[] references)
        {
            var map = new Dictionary<object, int>(_references.Length + references.Length);
            for (int i = 0; i < _references.Length; i++) map.Add(_references[i], map.Count);
            for (int i = 0; i < references.Length; i++) map.Add(references[i], map.Count);
            return new SerializeContext(writer, this, _world, map);
        }

        DeserializeContext Context(Reader reader, int count, params object[] references)
        {
            var array = new object[count];
            Array.Copy(_references, 0, array, 0, _references.Length);
            Array.Copy(references, 0, array, _references.Length, references.Length);
            return new DeserializeContext(reader, this, _world, array);
        }

        bool Deserialize(out object instance, Type type, int reference, in DeserializeContext context)
        {
            var serializer = Describe(type);
            if (serializer.Instantiate(out instance, context))
            {
                context.References[reference] = instance;
                if (serializer.Initialize(ref instance, context))
                {
                    context.References[reference] = instance;
                    return true;
                }
            }
            instance = default;
            return false;
        }

        bool Deserialize<T>(out T instance, int reference, in DeserializeContext context)
        {
            var serializer = Describe<T>();
            if (serializer.Instantiate(out instance, context))
            {
                context.References[reference] = instance;
                if (serializer.Initialize(ref instance, context))
                {
                    context.References[reference] = instance;
                    return true;
                }
            }
            instance = default;
            return false;
        }
    }
}