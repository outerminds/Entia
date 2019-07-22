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
            typeof(object).GetType(), typeof(object).Module.GetType(), typeof(object).Assembly.GetType(),
            typeof(Action), new Action(() => { }).Method.GetType(),
            typeof(AppDomain), typeof(Pointer),
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
                if (Serialize(instance, Context(writer, references)))
                {
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
                if (Serialize(instance, type, Context(writer, references)))
                {
                    bytes = writer.ToArray();
                    return true;
                }
                bytes = default;
                return false;
            }
        }
        public bool Serialize(object instance, Type type, in SerializeContext context) =>
            Describe(type).Serialize(instance, context);
        public bool Serialize<T>(in T instance, in SerializeContext context) => Describe<T>().Serialize<T>(instance, context);

        public bool Deserialize<T>(byte[] bytes, out T instance, params object[] references)
        {
            using (var reader = new Reader(bytes)) return Deserialize(out instance, Context(reader, references));
        }

        public bool Deserialize(byte[] bytes, out object instance, Type type, params object[] references)
        {
            using (var reader = new Reader(bytes)) return Deserialize(out instance, type, Context(reader, references));
        }

        public bool Deserialize(out object instance, Type type, in DeserializeContext context) =>
            Describe(type).Deserialize(out instance, context);
        public bool Deserialize<T>(out T instance, in DeserializeContext context) =>
            Describe<T>().Deserialize(out instance, context);

        public bool Clone(object instance, out object clone, Type type, in CloneContext context) =>
            Describe(type).Clone(instance, out clone, context);
        public bool Clone<T>(in T instance, out T clone, in CloneContext context)
        {
            var serializer = Describe<T>();
            if (serializer is Serializer<T> casted) return casted.Clone(instance, out clone, context);
            else if (serializer.Clone(instance, out var value, context))
            {
                clone = (T)value;
                return true;
            }
            clone = default;
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
            var map = new Dictionary<object, int>();
            for (int i = 0; i < _references.Length; i++) map.Add(_references[i], map.Count);
            for (int i = 0; i < references.Length; i++) map.Add(references[i], map.Count);
            return new SerializeContext(writer, this, _world, map);
        }

        DeserializeContext Context(Reader reader, params object[] references)
        {
            var list = new List<object>();
            for (int i = 0; i < _references.Length; i++) list.Add(_references[i]);
            for (int i = 0; i < references.Length; i++) list.Add(references[i]);
            return new DeserializeContext(reader, this, _world, list);
        }
    }
}