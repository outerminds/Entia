using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;
using Entia.Json.Converters;

namespace Entia.Json
{
    public readonly struct ConvertFromContext
    {
        // NOTE: do not use a 'hard' cast '(T)value' because in some cases, a non-null value
        // may be of the wrong type, for example in the case of type name collision
        static T Cast<T>(object value) => value is T casted ? casted : default;

        public readonly Node Node;
        public readonly TypeData Type;
        public readonly Dictionary<uint, object> References;
        public readonly Container Container;

        public ConvertFromContext(Dictionary<uint, object> references, Container container)
            : this(null, null, references, container) { }
        ConvertFromContext(Node node, TypeData type, Dictionary<uint, object> references, Container container)
        {
            Node = node;
            Type = type;
            Container = container;
            References = references;
        }

        public T Convert<T>(Node node) => Convert<T>(node, TypeUtility.GetData<T>());
        public object Convert(Node node, Type type) => Convert(node, TypeUtility.GetData(type));
        public T Convert<T>(Node node, TypeData type) =>
            TrySpecial(node, type, out var instance) ? Cast<T>(instance) :
            TryConverter<T>(node, type, out instance) ? Cast<T>(instance) :
            Default<T>(node, type);
        public object Convert(Node node, TypeData type) =>
            TrySpecial(node, type, out var instance) ? instance :
            TryConverter(node, type, out instance) ? instance :
            Default(node, type);

        public T Instantiate<T>() => Instantiate<T>(TypeUtility.GetData<T>());

        public object Instantiate(Type type) => Instantiate(TypeUtility.GetData(type));

        public T Instantiate<T>(TypeData type)
        {
            if (type.Type.IsValueType) return default;
            return Cast<T>(Instantiate(type));
        }

        public object Instantiate(TypeData type)
        {
            if (type.Type.IsAbstract) return type.Default;
            return
                CloneUtility.Shallow(type.Default) ??
                type.DefaultConstructor?.Invoke(Array.Empty<object>()) ??
                FormatterServices.GetUninitializedObject(type);
        }

        public ConvertFromContext With(Node node = null, TypeData type = null) =>
            new ConvertFromContext(node ?? Node, type ?? Type, References, Container);

        bool TrySpecial(Node node, TypeData type, out object instance)
        {
            switch (node.Kind)
            {
                case Node.Kinds.Null:
                    instance = null;
                    return true;
                case Node.Kinds.Type:
                    instance = node.AsType();
                    return true;
                case Node.Kinds.Reference:
                    References.TryGetValue(node.AsReference(), out instance);
                    return true;
                case Node.Kinds.Abstract:
                    instance = node.TryAbstract(out var concrete, out var value) ? Convert(value, concrete) : null;
                    return true;
                default:
                    switch (type.Code)
                    {
                        case TypeCode.Byte: instance = type.Type.IsEnum ? Enum.ToObject(type, node.AsByte()) : node.AsByte(); return true;
                        case TypeCode.SByte: instance = type.Type.IsEnum ? Enum.ToObject(type, node.AsSByte()) : node.AsSByte(); return true;
                        case TypeCode.Int16: instance = type.Type.IsEnum ? Enum.ToObject(type, node.AsShort()) : node.AsShort(); return true;
                        case TypeCode.Int32: instance = type.Type.IsEnum ? Enum.ToObject(type, node.AsInt()) : node.AsInt(); return true;
                        case TypeCode.Int64: instance = type.Type.IsEnum ? Enum.ToObject(type, node.AsLong()) : node.AsLong(); return true;
                        case TypeCode.UInt16: instance = type.Type.IsEnum ? Enum.ToObject(type, node.AsUShort()) : node.AsUShort(); return true;
                        case TypeCode.UInt32: instance = type.Type.IsEnum ? Enum.ToObject(type, node.AsUInt()) : node.AsUInt(); return true;
                        case TypeCode.UInt64: instance = type.Type.IsEnum ? Enum.ToObject(type, node.AsULong()) : node.AsULong(); return true;
                        case TypeCode.Single: instance = node.AsFloat(); return true;
                        case TypeCode.Double: instance = node.AsDouble(); return true;
                        case TypeCode.Decimal: instance = node.AsDecimal(); return true;
                        case TypeCode.Boolean: instance = node.AsBool(); return true;
                        case TypeCode.Char: instance = node.AsChar(); return true;
                        case TypeCode.String: instance = node.AsString(); return true;
                        case TypeCode.Object when type.Definition == typeof(Nullable<>):
                            instance = Convert(node, type.Element);
                            return true;
                        default: instance = default; return false;
                    }
            }
        }

        bool TryConverter<T>(Node node, TypeData type, out object instance)
        {
            if (Container.TryGet<T, IConverter>(out var converter))
            {
                instance = Instantiate(node, type, converter);
                return true;
            }

            instance = default;
            return false;
        }

        bool TryConverter(Node node, TypeData type, out object instance)
        {
            if (Container.TryGet<IConverter>(type, out var converter))
            {
                instance = Instantiate(node, type, converter);
                return true;
            }

            instance = default;
            return false;
        }

        object Instantiate(Node node, TypeData type, IConverter converter)
        {
            var context = With(node, type);
            var instance = References[node.Identifier] = converter.Instantiate(context);
            converter.Initialize(ref instance, context);
            // NOTE: must be set again in the case where the boxed instance is unboxed and reboxed
            References[node.Identifier] = instance;
            return instance;
        }

        T Default<T>(Node node, TypeData type)
        {
            // NOTE: instance must be boxed to ensure it is not copied around if it is a struct
            var instance = References[node.Identifier] = Instantiate<T>(type);
            Initialize(instance, node, type);
            return Cast<T>(instance);
        }

        object Default(Node node, TypeData type)
        {
            var instance = References[node.Identifier] = Instantiate(type);
            Initialize(instance, node, type);
            return instance;
        }

        void Initialize(object instance, Node node, TypeData type)
        {
            if (instance == null) return;

            var members = type.InstanceMembers;
            foreach (var (key, value) in node.Members())
            {
                if (members.TryGetValue(key, out var member))
                {
                    if (member is FieldInfo field)
                        field.SetValue(instance, Convert(value, field.FieldType));
                    else if (member is PropertyInfo property && property.CanWrite)
                        property.SetValue(instance, Convert(value, property.PropertyType));
                }
            }
        }
    }
}