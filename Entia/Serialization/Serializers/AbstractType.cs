using System;
using System.Collections.Generic;
using System.Reflection;
using Entia.Core;
using Entia.Experimental.Serialization;
using Entia.Modules.Message;
using Entia.Modules.Group;
using Entia.Modules.Query;
using System.Linq;

namespace Entia.Experimental.Serializers
{
    public sealed class AbstractType : Serializer<Type>
    {
        enum Kinds : byte { None, Type, Array, RankArray, Pointer, Generic, Reference }

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

        static readonly Type[] _references = {
            #region System
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
            typeof(Action), typeof(Action[]),
            typeof(object), typeof(object[]),

            typeof(Nullable<>),
            typeof(List<>), typeof(LinkedList<>), typeof(LinkedListNode<>),
            typeof(Stack<>), typeof(Queue<>), typeof(HashSet<>), typeof(Dictionary<,>),
            typeof(SortedDictionary<,>), typeof(SortedList<,>), typeof(SortedSet<>),
            typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>), typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>),
            typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>), typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>),
            typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>), typeof(Action<,,,,>), typeof(Action<,,,,,>), typeof(Action<,,,,,,>),
            typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>), typeof(Func<,,,,,>), typeof(Func<,,,,,,>),
            typeof(Predicate<>), typeof(Comparison<>),
            #endregion

            #region Reflection
            typeof(object).GetType(), typeof(object).Module.GetType(), typeof(object).Assembly.GetType(),
            typeof(Members).GetField(nameof(Members.Field)).GetType(),
            typeof(Members).GetProperty(nameof(Members.Property)).GetType(),
            typeof(Members).GetMethod(nameof(Members.Method)).GetType(),
            typeof(Pointer),
            #endregion

            #region Entia.Core
            typeof(Unit), typeof(Unit[]), typeof(Unit*), typeof(Unit?),
            typeof(BitMask), typeof(Disposable),

            typeof(Concurrent<>),
            typeof(Option<>), typeof(Result<>),
            typeof(Box<>), typeof(Box<>.Read),
            typeof(Slice<>), typeof(Slice<>.Read),
            typeof(TypeMap<,>),
            typeof(Disposable<>),
            #endregion

            #region Entia
            typeof(World),
            typeof(Entity), typeof(Entity[]), typeof(Entity*), typeof(Entity?),
            typeof(Query),

            typeof(Emitter<>), typeof(Receiver<>), typeof(Reaction<>),
            typeof(Group<>), typeof(Query<>),
            #endregion
        };
        static readonly Dictionary<Type, ushort> _indices = _references
            .Select((type, index) => (type, index))
            .ToDictionary(pair => pair.type, pair => (ushort)pair.index);

        public override bool Serialize(in Type instance, in SerializeContext context)
        {
            if (_indices.TryGetValue(instance, out var index))
            {
                context.Writer.Write(Kinds.Reference);
                context.Writer.Write(index);
                return true;
            }
            else if (instance.IsArray)
            {
                var rank = instance.GetArrayRank();
                var element = instance.GetElementType();
                if (rank > 1)
                {
                    context.Writer.Write(Kinds.RankArray);
                    context.Writer.Write((byte)rank);
                }
                else context.Writer.Write(Kinds.Array);
                return Serialize(element, context);
            }
            else if (instance.IsPointer)
            {
                context.Writer.Write(Kinds.Pointer);
                var element = instance.GetElementType();
                return Serialize(element, context);
            }
            else if (instance.IsConstructedGenericType)
            {
                context.Writer.Write(Kinds.Generic);
                var definition = instance.GetGenericTypeDefinition();
                var arguments = instance.GetGenericArguments();
                if (Serialize(definition, context))
                {
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        if (Serialize(arguments[i], context)) continue;
                        return false;
                    }
                    return true;
                }
                return false;
            }
            else
            {
                context.Writer.Write(Kinds.Type);
                context.Writer.Write(instance.MetadataToken);
                return context.Serialize(instance.Module, instance.Module.GetType());
            }
        }

        public override bool Instantiate(out Type instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out Kinds kind))
            {
                switch (kind)
                {
                    case Kinds.Reference:
                        {
                            if (context.Reader.Read(out ushort index))
                            {
                                instance = _references[index];
                                return true;
                            }
                            break;
                        }
                    case Kinds.Array:
                        {
                            if (Deserialize(out var element, context))
                            {
                                instance = element.MakeArrayType();
                                return true;
                            }
                            break;
                        }
                    case Kinds.RankArray:
                        {
                            if (context.Reader.Read(out byte rank) && Deserialize(out var element, context))
                            {
                                instance = element.MakeArrayType(rank);
                                return true;
                            }
                            break;
                        }
                    case Kinds.Pointer:
                        {
                            if (Deserialize(out var element, context))
                            {
                                instance = element.MakePointerType();
                                return true;
                            }
                            break;
                        }
                    case Kinds.Generic:
                        {
                            if (Deserialize(out instance, context))
                            {
                                var arguments = instance.GetGenericArguments();
                                for (int i = 0; i < arguments.Length; i++)
                                {
                                    if (Deserialize(out arguments[i], context)) continue;
                                    return false;
                                }
                                instance = instance.MakeGenericType(arguments);
                                return true;
                            }
                            break;
                        }
                    case Kinds.Type:
                        {
                            if (context.Reader.Read(out int token) && context.Deserialize(out Module module))
                            {
                                instance = module.ResolveType(token);
                                return true;
                            }
                            break;
                        }
                    default: instance = default; return false;
                }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Type instance, in DeserializeContext context) => true;
    }
}