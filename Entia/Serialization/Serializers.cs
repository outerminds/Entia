using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Entia.Core;
using Entia.Modules.Serialization;
using Entia.Serializables;
using Entia.Serializers;

namespace Entia.Modules
{
    public readonly struct WriteContext
    {
        public readonly Writer Writer;
        public readonly World World;
        public readonly Dictionary<object, int> References;
        public readonly Serializers Serializers;

        public WriteContext(Writer writer, Dictionary<object, int> references, World world)
        {
            Writer = writer;
            References = references;
            World = world;
            Serializers = world.Serializers();
        }
    }

    public readonly struct ReadContext
    {
        public readonly Reader Reader;
        public readonly World World;
        public readonly List<object> References;
        public readonly Serializers Serializers;

        public ReadContext(Reader reader, List<object> references, World world)
        {
            Reader = reader;
            References = references;
            World = world;
            Serializers = world.Serializers();
        }
    }

    public enum Kinds : byte { Null = 1, Reference, Value, TypedValue }

    public unsafe sealed class Serializers : IModule, IEnumerable<ISerializer>
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
        readonly TypeMap<object, ISerializer> _defaults = new TypeMap<object, ISerializer>(
            (typeof(System.Delegate), new Entia.Serializers.Delegate()),
            (typeof(System.Array), new Entia.Serializers.Array()),
            (typeof(System.Reflection.Assembly), new Entia.Serializers.Assembly()),
            (typeof(System.Reflection.Module), new Entia.Serializers.Module()),
            (typeof(System.Type), new Entia.Serializers.Type()),
            (typeof(MethodBase), new Method()),
            (typeof(MemberInfo), new Member()),
            (typeof(ReaderWriterLockSlim), new Entia.Serializers.ReaderWriterLock())
        );

        public Serializers(World world) { _world = world; }

        public bool Serialize<T>(in T value, out byte[] bytes, params object[] references)
        {
            using (var writer = new Writer())
            {
                var context = new WriteContext(writer, new Dictionary<object, int>(8), _world);
                for (int i = 0; i < _references.Length; i++) context.References[_references[i]] = context.References.Count;
                for (int i = 0; i < references.Length; i++) context.References[references[i]] = context.References.Count;
                if (Serialize(value, context))
                {
                    bytes = writer.ToArray();
                    return true;
                }
                bytes = System.Array.Empty<byte>();
                return false;
            }
        }

        public bool Serialize<T>(in T value, in WriteContext context) =>
            Serialize((object)value, TypeUtility.Cache<T>.Data, context);
        public bool Serialize(object value, TypeData @static, in WriteContext context) =>
            Serialize(value, value?.GetType() ?? @static, @static, context);

        public bool Deserialize<T>(byte[] bytes, out T value, params object[] references)
        {
            using (var reader = new Reader(bytes))
            {
                var context = new ReadContext(reader, new List<object>(8), _world);
                for (int i = 0; i < _references.Length; i++) context.References.Add(_references[i]);
                for (int i = 0; i < references.Length; i++) context.References.Add(references[i]);
                return Deserialize(out value, context);
            }
        }

        public bool Deserialize<T>(out T value, in ReadContext context)
        {
            if (Deserialize(out var current, TypeUtility.Cache<T>.Data, context) && current is T casted)
            {
                value = casted;
                return true;
            }
            value = default;
            return false;
        }
        public bool Deserialize(out object value, TypeData @static, in ReadContext context)
        {
            context.Reader.Read(out Kinds kind);
            switch (kind)
            {
                case Kinds.Reference:
                    context.Reader.Read(out ushort reference);
                    value = context.References[reference];
                    return true;
                case Kinds.Null: value = null; return true;
                case Kinds.Value: return Deserialize(out value, @static, @static, context);
                case Kinds.TypedValue:
                    if (Deserialize(out System.Type dynamic, context) && Deserialize(out value, dynamic, @static, context))
                        return true;
                    value = default;
                    return false;
                default:
                    value = default;
                    return false;
            }
        }

        public ISerializer Default<T>() => _defaults.TryGet<T>(out var serializer, false, false) ? serializer : Default(typeof(T));
        public ISerializer Default(System.Type type) => _defaults.Default(type, typeof(ISerializable<>), typeof(SerializerAttribute), _ => new Default());
        public bool Has<T>() => _serializers.Has<T>(true, false);
        public bool Has(System.Type type) => _serializers.Has(type, true, false);
        public ISerializer Get<T>() => _serializers.TryGet<T>(out var serializer, true, false) ? serializer : Default<T>();
        public ISerializer Get(System.Type type) => _serializers.TryGet(type, out var serializer, true, false) ? serializer : Default(type);
        public bool Set<T>(ISerializer serializer) => _serializers.Set<T>(serializer);
        public bool Set(System.Type type, ISerializer serializer) => _serializers.Set(type, serializer);
        public bool Remove<T>() => _serializers.Remove<T>(false, false);
        public bool Remove(System.Type type) => _serializers.Remove(type, false, false);
        public bool Clear() => _defaults.Clear() | _serializers.Clear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<ISerializer> GetEnumerator() => _serializers.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool Serialize(object value, TypeData dynamic, TypeData @static, in WriteContext context)
        {
            if (value == null)
            {
                context.Writer.Write(Kinds.Null);
                return true;
            }
            else if (context.References.TryGetValue(value, out var reference))
            {
                context.Writer.Write(Kinds.Reference);
                context.Writer.Write((ushort)reference);
                return true;
            }
            else if (dynamic == @static)
            {
                context.Writer.Write(Kinds.Value);
                context.References[value] = context.References.Count;
                return Get(dynamic.Type).Serialize(value, dynamic, @static, context);
            }
            else
            {
                context.Writer.Write(Kinds.TypedValue);
                Serialize(dynamic.Type, dynamic.Type.GetType(), context);
                context.References[value] = context.References.Count;
                return Get(dynamic).Serialize(value, dynamic, @static, context);
            }
        }

        bool Deserialize(out object value, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var serializer = Get(dynamic);
            var index = context.References.Count;
            context.References.Add(default);
            if (serializer.Instantiate(out value, dynamic, @static, context))
            {
                context.References[index] = value;
                if (serializer.Deserialize(ref value, dynamic, @static, context))
                {
                    context.References[index] = value;
                    return true;
                }
            }
            return false;
        }
    }
}