using System;
using System.Collections.Generic;
using System.Reflection;
using Entia.Core;
using Entia.Serializers;
using System.Runtime.InteropServices;

namespace Entia.Serialization
{
    public enum Kinds : byte { None, Null, Abstract, Concrete, Reference }

    public readonly struct SerializeContext
    {
        public readonly Type Type;
        public readonly Writer Writer;
        public readonly Dictionary<object, int> References;
        public readonly World World;

        public SerializeContext(Writer writer, Dictionary<object, int> references, World world) : this(null, writer, references, world) { }
        public SerializeContext(Type type, Writer writer, Dictionary<object, int> references, World world)
        {
            Type = type;
            Writer = writer;
            References = references;
            World = world;
        }

        public bool Serialize(object instance, Type type, ISerializer serializer = null)
        {
            if (instance is null)
            {
                Writer.Write(Kinds.Null);
                return true;
            }
            else if (References.TryGetValue(instance, out var index))
            {
                Writer.Write(Kinds.Reference);
                Writer.Write(index);
                return true;
            }

            var dynamic = instance.GetType();
            var reference = References[instance] = References.Count;
            if (type == dynamic)
            {
                Writer.Write(Kinds.Concrete);
                Writer.Write(reference);
                return TrySerialize(instance, type, serializer);
            }
            else
            {
                Writer.Write(Kinds.Abstract);
                Writer.Write(reference);
                return Serialize(dynamic, dynamic.GetType()) && TrySerialize(instance, dynamic, serializer);
            }
        }

        public bool Serialize<T>(in T instance, ISerializer serializer = null)
        {
            var box = (object)instance;
            if (box is null)
            {
                Writer.Write(Kinds.Null);
                return true;
            }
            else if (References.TryGetValue(box, out var index))
            {
                Writer.Write(Kinds.Reference);
                Writer.Write(index);
                return true;
            }

            var dynamic = instance.GetType();
            var reference = References[box] = References.Count;
            if (typeof(T) == dynamic)
            {
                Writer.Write(Kinds.Concrete);
                Writer.Write(reference);
                return TrySerialize<T>(instance, serializer);
            }
            else
            {
                Writer.Write(Kinds.Abstract);
                Writer.Write(reference);
                return Serialize(dynamic, dynamic.GetType()) && TrySerialize(box, dynamic, serializer);
            }
        }

        public SerializeContext With<T>() => With(typeof(T));
        public SerializeContext With(Type type = null) => new SerializeContext(type ?? Type, Writer, References, World);

        bool TrySerialize<T>(in T instance, ISerializer serializer)
        {
            if (serializer is null) return World.Container.TryGet<T, ISerializer>(out serializer) && serializer.Serialize<T>(instance, With<T>());
            return serializer.Serialize<T>(instance, With<T>());
        }

        bool TrySerialize(object instance, Type type, ISerializer serializer)
        {
            if (serializer is null) return World.Container.TryGet<ISerializer>(type, out serializer) && serializer.Serialize(instance, With(type));
            return serializer.Serialize(instance, With(type));
        }
    }

    public readonly struct DeserializeContext
    {
        public readonly Type Type;
        public readonly Reader Reader;
        public readonly object[] References;
        public readonly World World;

        public DeserializeContext(Reader reader, object[] references, World world) : this(null, reader, references, world) { }
        public DeserializeContext(Type type, Reader reader, object[] references, World world)
        {
            Type = type;
            Reader = reader;
            References = references;
            World = world;
        }

        public bool Deserialize(out object instance, Type type, ISerializer serializer = null)
        {
            if (Reader.Read(out Kinds kind))
            {
                switch (kind)
                {
                    case Kinds.Null: instance = default; return true;
                    case Kinds.Reference:
                        if (Reader.Read(out int index))
                        {
                            instance = References[index];
                            return true;
                        }
                        break;
                    case Kinds.Abstract:
                        {
                            if (Reader.Read(out int reference) && Deserialize(out Type dynamic))
                                return TryDeserialize(out instance, dynamic, reference, serializer);
                            break;
                        }
                    case Kinds.Concrete:
                        {
                            if (Reader.Read(out int reference)) return TryDeserialize(out instance, type, reference, serializer);
                            break;
                        }
                }
            }
            instance = default;
            return false;
        }

        public bool Deserialize<T>(out T instance, ISerializer serializer = null)
        {
            if (Reader.Read(out Kinds kind))
            {
                switch (kind)
                {
                    case Kinds.Null: instance = default; return true;
                    case Kinds.Reference:
                        if (Reader.Read(out int index))
                        {
                            instance = (T)References[index];
                            return true;
                        }
                        break;
                    case Kinds.Abstract:
                        {
                            if (Reader.Read(out int reference) &&
                                Deserialize(out Type dynamic) &&
                                TryDeserialize(out var value, dynamic, reference, serializer))
                            {
                                instance = (T)value;
                                return true;
                            }
                            break;
                        }
                    case Kinds.Concrete:
                        {
                            if (Reader.Read(out int reference)) return TryDeserialize(out instance, reference, serializer);
                            break;
                        }
                }
            }
            instance = default;
            return false;
        }

        public DeserializeContext With<T>() => With(typeof(T));
        public DeserializeContext With(Type type = null) => new DeserializeContext(type ?? Type, Reader, References, World);

        bool TryDeserialize(out object instance, Type type, int reference, ISerializer serializer)
        {
            var context = With(type);
            if ((serializer is ISerializer || World.Container.TryGet<ISerializer>(type, out serializer)) &&
                serializer.Instantiate(out instance, context))
            {
                References[reference] = instance;
                if (serializer.Initialize(ref instance, context))
                {
                    References[reference] = instance;
                    return true;
                }
            }
            instance = default;
            return false;
        }

        bool TryDeserialize<T>(out T instance, int reference, ISerializer serializer)
        {
            var context = With<T>();
            if ((serializer is ISerializer || World.Container.TryGet<T, ISerializer>(out serializer)) &&
                serializer.Instantiate(out instance, context))
            {
                References[reference] = instance;
                if (serializer.Initialize(ref instance, context))
                {
                    References[reference] = instance;
                    return true;
                }
            }
            instance = default;
            return false;
        }
    }

    public static class Extensions
    {
        [Preserve]
        readonly struct Members
        {
            [Preserve]
            public readonly object Field;
            [Preserve]
            public object Property { get; }
            [Preserve]
            public void Method() { }
            [Preserve]
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

            typeof(World),
            typeof(Entity), typeof(Entity[]), typeof(Entity*), typeof(Entity?),
            typeof(BitMask), typeof(Unit), typeof(Disposable)
        };

        public static bool Serialize<T>(this World world, in T instance, out byte[] bytes, params object[] references)
        {
            using (var writer = new Writer())
            {
                var count = writer.Reserve<int>();
                var context = world.Context(writer, references);
                if (context.Serialize(instance))
                {
                    count.Value = context.References.Count;
                    bytes = writer.ToArray();
                    return true;
                }
                bytes = default;
                return false;
            }
        }

        public static bool Serialize(this World world, object instance, Type type, out byte[] bytes, params object[] references)
        {
            using (var writer = new Writer())
            {
                var count = writer.Reserve<int>();
                var context = world.Context(writer, references);
                if (context.Serialize(instance, type))
                {
                    count.Value = context.References.Count;
                    bytes = writer.ToArray();
                    return true;
                }
                bytes = default;
                return false;
            }
        }

        public static bool Deserialize<T>(this World world, byte[] bytes, out T instance, params object[] references)
        {
            using (var reader = new Reader(bytes))
            {
                reader.Read(out int count);
                return world.Context(reader, count, references).Deserialize(out instance);
            }
        }

        public static bool Deserialize(this World world, byte[] bytes, out object instance, Type type, params object[] references)
        {
            using (var reader = new Reader(bytes))
            {
                reader.Read(out int count);
                return world.Context(reader, count, references).Deserialize(out instance, type);
            }
        }

        public static bool Serialize<T>(this ISerializer serializer, in T instance, SerializeContext context)
        {
            if (serializer is Serializer<T> casted) return casted.Serialize(instance, context);
            return serializer.Serialize(instance, context);
        }

        public static bool Deserialize<T>(this ISerializer serializer, out T instance, DeserializeContext context)
        {
            if (serializer is Serializer<T> casted) return casted.Deserialize(out instance, context);
            else if (serializer.Deserialize(out var value, context))
            {
                instance = (T)value;
                return true;
            }
            else
            {
                instance = default;
                return false;
            }
        }

        public static bool Instantiate<T>(this ISerializer serializer, out T instance, DeserializeContext context)
        {
            if (serializer is Serializer<T> casted) return casted.Instantiate(out instance, context);
            else if (serializer.Instantiate(out var value, context))
            {
                instance = (T)value;
                return true;
            }
            else
            {
                instance = default;
                return false;
            }
        }

        public static bool Initialize<T>(this ISerializer serializer, ref T instance, DeserializeContext context)
        {
            if (serializer is Serializer<T> casted) return casted.Initialize(ref instance, context);

            object value = instance;
            if (serializer.Initialize(ref value, context))
            {
                instance = (T)value;
                return true;
            }
            else
            {
                instance = default;
                return false;
            }
        }

        public static bool Deserialize(this ISerializer serializer, out object instance, DeserializeContext context) =>
            serializer.Instantiate(out instance, context) && serializer.Initialize(ref instance, context);

        public static unsafe void Fix(this byte[] bytes, out GCHandle handle, out byte* pointer)
        {
            handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            pointer = (byte*)handle.AddrOfPinnedObject();
        }

        static SerializeContext Context(this World world, Writer writer, params object[] references)
        {
            var map = new Dictionary<object, int>(_references.Length + references.Length);
            for (int i = 0; i < _references.Length; i++) map.Add(_references[i], map.Count);
            for (int i = 0; i < references.Length; i++) map.Add(references[i], map.Count);
            return new SerializeContext(writer, map, world);
        }

        static DeserializeContext Context(this World world, Reader reader, int count, params object[] references)
        {
            var array = new object[count];
            Array.Copy(_references, 0, array, 0, _references.Length);
            Array.Copy(references, 0, array, _references.Length, references.Length);
            return new DeserializeContext(reader, array, world);
        }
    }
}