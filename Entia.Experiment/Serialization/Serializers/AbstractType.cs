using System;
using System.Collections.Generic;
using System.Reflection;
using Entia.Core;
using Entia.Modules.Message;

namespace Entia.Experiment
{
    public sealed class AbstractType : Serializer<Type>
    {
        enum Kinds : byte { None, Type, Array, Pointer, Generic, Definition }

        static readonly Type[] _definitions = {
            #region System
            typeof(Nullable<>),
            typeof(List<>),
            typeof(Dictionary<,>),
            typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>), typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>),
            typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>), typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>),
            typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>), typeof(Action<,,,,>), typeof(Action<,,,,,>), typeof(Action<,,,,,,>),
            typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>), typeof(Func<,,,,,>), typeof(Func<,,,,,,>),
            #endregion

            #region Entia.Core
            typeof(Concurrent<>),
            typeof(Option<>), typeof(Result<>),
            typeof(Box<>), typeof(Box<>.Read),
            typeof(Slice<>), typeof(Slice<>.Read),
            typeof(SwissList<>), typeof(SwitchList<>), typeof(TypeMap<,>),
            typeof(Disposable<>),
            #endregion

            #region Entia
            typeof(Emitter<>), typeof(Receiver<>), typeof(Reaction<>),
            #endregion
        };

        public override bool Serialize(in Type instance, in SerializeContext context)
        {
            if (instance.IsArray)
            {
                var rank = instance.GetArrayRank();
                var element = instance.GetElementType();
                context.Writer.Write(Kinds.Array);
                context.Writer.Write((byte)rank);
                return Serialize(element, context);
            }
            else if (instance.IsPointer)
            {
                context.Writer.Write(Kinds.Pointer);
                var element = instance.GetElementType();
                return Serialize(element, context);
            }
            else if (instance.IsGenericTypeDefinition)
            {
                var index = Array.IndexOf(_definitions, instance);
                if (index >= 0)
                {
                    context.Writer.Write(Kinds.Definition);
                    context.Writer.Write((byte)index);
                    return true;
                }
                else return SerializeType(instance, context);
            }
            else if (instance.IsGenericType)
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
            else return SerializeType(instance, context);
        }

        public override bool Instantiate(out Type instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out Kinds kind))
            {
                switch (kind)
                {
                    case Kinds.Array:
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
                    case Kinds.Definition:
                        {
                            if (context.Reader.Read(out byte index))
                            {
                                instance = _definitions[index];
                                return true;
                            }
                            break;
                        }
                    case Kinds.Type:
                        {
                            if (context.Reader.Read(out int token) && context.Descriptors.Deserialize(out Module module, context))
                            {
                                instance = module.ResolveType(token);
                                return true;
                            }
                            break;
                        }
                    default:
                        instance = default; return false;
                }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Type instance, in DeserializeContext context) => true;

        bool SerializeType(Type instance, in SerializeContext context)
        {
            context.Writer.Write(Kinds.Type);
            context.Writer.Write(instance.MetadataToken);
            return context.Descriptors.Serialize(instance.Module, instance.Module.GetType(), context);
        }
    }
}