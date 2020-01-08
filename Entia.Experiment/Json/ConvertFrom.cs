using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Entia.Core;
using Entia.Experiment.Json.Converters;

namespace Entia.Experiment.Json
{
    public readonly struct ConvertFromContext
    {
        public readonly Node Node;
        public readonly TypeData Type;
        public readonly List<object> References;
        public readonly Container Container;

        public ConvertFromContext(List<object> references, Container container = null)
            : this(Node.Null, null, references, container) { }
        public ConvertFromContext(Node node, TypeData type, List<object> references, Container container = null)
        {
            Node = node;
            Type = type;
            References = references;
            Container = container ?? new Container();
        }

        public T Convert<T>(Node node) =>
            TrySpecial(node, out var special) ? (T)special : Concrete<T>(node);
        public object Convert(Node node, TypeData type) =>
            TrySpecial(node, out var special) ? special : Concrete(node, type);
        public object Convert(Node node, Type type) =>
            TrySpecial(node, out var special) ? special : Concrete(node, type);

        public ConvertFromContext With(Node node, TypeData type) => new ConvertFromContext(node, type, References, Container);

        T Concrete<T>(Node node)
        {
            var data = TypeUtility.GetData<T>();
            if (TryPrimitive(node, data, out var primitive)) return (T)primitive;
            if (TryConverter<T>(node, data, out var instance)) return (T)instance;
            return (T)Default(node, data);
        }

        object Concrete(Node node, TypeData type)
        {
            if (TryPrimitive(node, type, out var primitive)) return primitive;
            if (TryConverter(node, type, out var instance)) return instance;
            return Default(node, type);
        }

        bool TrySpecial(Node node, out object instance)
        {
            if (node.Kind == Node.Kinds.Null)
            {
                instance = null;
                return true;
            }
            else if (node.TryReference(out var reference))
            {
                instance = References[int.Parse(reference.Value)];
                return true;
            }
            else if (node.TryAbstract(out var type, out var value))
            {
                instance = Convert(value, Convert<Type>(type));
                return true;
            }

            instance = default;
            return false;
        }

        bool TryPrimitive(Node node, TypeData type, out object instance)
        {
            switch (type.Code)
            {
                case TypeCode.Char: instance = char.Parse(node.Value); return true;
                case TypeCode.Byte: instance = byte.Parse(node.Value); return true;
                case TypeCode.SByte: instance = sbyte.Parse(node.Value); return true;
                case TypeCode.Int16: instance = short.Parse(node.Value); return true;
                case TypeCode.Int32: instance = int.Parse(node.Value); return true;
                case TypeCode.Int64: instance = long.Parse(node.Value); return true;
                case TypeCode.UInt16: instance = ushort.Parse(node.Value); return true;
                case TypeCode.UInt32: instance = uint.Parse(node.Value); return true;
                case TypeCode.UInt64: instance = ulong.Parse(node.Value); return true;
                case TypeCode.Single: instance = float.Parse(node.Value); return true;
                case TypeCode.Double: instance = double.Parse(node.Value); return true;
                case TypeCode.Decimal: instance = decimal.Parse(node.Value); return true;
                case TypeCode.Boolean: instance = bool.Parse(node.Value); return true;
                case TypeCode.String: instance = node.Value; return true;
                default: instance = default; return false;
            }
        }

        bool TryConverter<T>(Node node, TypeData type, out object instance)
        {
            foreach (var converter in Container.Get<T, IConverter>())
            {
                if (converter.CanConvert(type))
                {
                    var index = References.Count;
                    References.Add(default);
                    instance = converter.Instantiate(With(node, type));
                    References[index] = instance;
                    converter.Initialize(ref instance, With(node, type));
                    References[index] = instance;
                    return true;
                }
            }

            instance = default;
            return false;
        }

        bool TryConverter(Node node, TypeData type, out object instance)
        {
            foreach (var converter in Container.Get<IConverter>(type))
            {
                if (converter.CanConvert(type))
                {
                    var index = References.Count;
                    References.Add(default);
                    instance = converter.Instantiate(With(node, type));
                    References[index] = instance;
                    converter.Initialize(ref instance, With(node, type));
                    References[index] = instance;
                    return true;
                }
            }

            instance = default;
            return false;
        }

        object Default(Node node, TypeData type)
        {
            var @object = FormatterServices.GetUninitializedObject(type);
            References.Add(@object);
            foreach (var child in node.Children)
            {
                if (child.TryMember(out var key, out var value) &&
                    type.Fields.TryGetValue(key.Value, out var field))
                    field.SetValue(@object, Convert(value, field.FieldType));
            }
            return @object;
        }
    }

    public static partial class Serialization
    {
        public static T Instantiate<T>(Node node, Container container = null, params object[] references) =>
            FromContext(references, container).Convert<T>(node);
        public static object Instantiate(Node node, Type type, Container container = null, params object[] references) =>
            FromContext(references, container).Convert(node, type);

        static ConvertFromContext FromContext(object[] references, Container container)
        {
            var list = new List<object>(references.Length);
            for (int i = 0; i < references.Length; i++) list.Add(references[i]);
            return new ConvertFromContext(list, container);
        }
    }
}