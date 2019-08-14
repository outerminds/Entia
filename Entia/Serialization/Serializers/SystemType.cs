using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Modules;

namespace Entia.Serializers
{
    public sealed class SystemType : Serializer<Type>
    {
        public enum Kinds : byte { Type = 1, Array, Pointer, Generic, Definition }

        static readonly Type[] _definitions ={
            typeof(Nullable<>),
            typeof(List<>),
            typeof(Dictionary<,>),
            typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>), typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>),
            typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>), typeof(Action<,,,,>), typeof(Action<,,,,,>), typeof(Action<,,,,,,>),
            typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>), typeof(Func<,,,,,>), typeof(Func<,,,,,,>),
            typeof(Concurrent<>),
            typeof(Option<>), typeof(Result<>),
            typeof(Box<>), typeof(Box<>.Read),
            typeof(Slice<>), typeof(Slice<>.Read),
            typeof(TypeMap<,>),
            typeof(Disposable<>)
        };

        public override bool Serialize(in Type instance, TypeData dynamic, TypeData @static, in WriteContext context)
        {
            if (instance.IsArray)
            {
                var rank = instance.GetArrayRank();
                var element = instance.GetElementType();
                context.Writer.Write(Kinds.Array);
                context.Writer.Write((byte)rank);
                return context.Serializers.Serialize(element, element.GetType(), context);
            }
            else if (instance.IsPointer)
            {
                context.Writer.Write(Kinds.Pointer);
                var element = instance.GetElementType();
                return context.Serializers.Serialize(element, element.GetType(), context);
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
                var success = context.Serializers.Serialize(definition, definition.GetType(), context);
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    success &= context.Serializers.Serialize(argument, argument.GetType(), context);
                }
                return success;
            }
            else return SerializeType(instance, context);
        }

        public override bool Instantiate(out Type instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var success = context.Reader.Read(out Kinds kind);
            switch (kind)
            {
                case Kinds.Array:
                    {
                        success &= context.Reader.Read(out byte rank);
                        success &= context.Serializers.Deserialize(out Type element, context);
                        instance = element.MakeArrayType(rank);
                        break;
                    }
                case Kinds.Pointer:
                    {
                        success &= context.Serializers.Deserialize(out Type element, context);
                        instance = element.MakePointerType();
                        break;
                    }
                case Kinds.Generic:
                    {
                        success &= context.Serializers.Deserialize(out Type definition, context);
                        var arguments = definition.GetGenericArguments();
                        for (int i = 0; i < arguments.Length; i++) success &= context.Serializers.Deserialize(out arguments[i], context);
                        instance = definition.MakeGenericType(arguments);
                        break;
                    }
                case Kinds.Definition:
                    {
                        success &= context.Reader.Read(out byte index);
                        instance = _definitions[index];
                        break;
                    }
                case Kinds.Type:
                    {
                        success &= context.Reader.Read(out int token);
                        success &= context.Serializers.Deserialize(out System.Reflection.Module module, context);
                        instance = module.ResolveType(token);
                        break;
                    }
                default: instance = default; return false;
            }
            return success;
        }

        public override bool Deserialize(ref Type instance, TypeData dynamic, TypeData @static, in ReadContext context) => true;

        bool SerializeType(Type instance, in WriteContext context)
        {
            context.Writer.Write(Kinds.Type);
            context.Writer.Write(instance.MetadataToken);
            return context.Serializers.Serialize(instance.Module, instance.Module.GetType(), context);
        }
    }
}