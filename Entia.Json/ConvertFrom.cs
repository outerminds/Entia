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
            TrySpecial(node, out var special) ? Cast<T>(special) : Concrete<T>(node);
        public object Convert(Node node, TypeData type) =>
            TrySpecial(node, out var special) ? special : Concrete(node, type);
        public object Convert(Node node, Type type) =>
            TrySpecial(node, out var special) ? special : Concrete(node, type);

        public T Instantiate<T>() => Instantiate<T>(TypeUtility.GetData<T>());

        public object Instantiate(TypeData type)
        {
            if (type.Type.IsAbstract) return type.Default;
            return
                CloneUtility.Shallow(type.Default) ??
                type.DefaultConstructor?.Invoke(Array.Empty<object>()) ??
                FormatterServices.GetUninitializedObject(type);
        }

        public object Instantiate(Type type) => Instantiate(TypeUtility.GetData(type));

        public ConvertFromContext With(Node node = null, TypeData type = null) =>
            new ConvertFromContext(node ?? Node, type ?? Type, References, Container);

        T Concrete<T>(Node node)
        {
            var type = TypeUtility.GetData<T>();
            if (TryPrimitive(node, type, out var primitive)) return Cast<T>(primitive);
            if (TryConverter<T>(node, type, out var instance)) return Cast<T>(instance);
            return Cast<T>(Initialize(Instantiate<T>(type), node, type));
        }

        object Concrete(Node node, TypeData type)
        {
            if (TryPrimitive(node, type, out var primitive)) return primitive;
            if (TryConverter(node, type, out var instance)) return instance;
            return Initialize(Instantiate(type), node, type);
        }

        bool TrySpecial(Node node, out object instance)
        {
            if (node.IsNull())
            {
                instance = null;
                return true;
            }
            else if (node.TryReference(out var reference))
            {
                instance = References[reference];
                return true;
            }
            else if (node.TryAbstract(out var type, out var value) && Convert<Type>(type) is Type concrete)
            {
                instance = Convert(value, concrete);
                return true;
            }

            instance = default;
            return false;
        }

        bool TryPrimitive(Node node, TypeData type, out object instance)
        {
            switch (type.Code)
            {
                case TypeCode.Byte: instance = node.AsByte(); return true;
                case TypeCode.SByte: instance = node.AsSByte(); return true;
                case TypeCode.Int16: instance = node.AsShort(); return true;
                case TypeCode.Int32: instance = node.AsInt(); return true;
                case TypeCode.Int64: instance = node.AsLong(); return true;
                case TypeCode.UInt16: instance = node.AsUShort(); return true;
                case TypeCode.UInt32: instance = node.AsUInt(); return true;
                case TypeCode.UInt64: instance = node.AsULong(); return true;
                case TypeCode.Single: instance = node.AsFloat(); return true;
                case TypeCode.Double: instance = node.AsDouble(); return true;
                case TypeCode.Decimal: instance = node.AsDecimal(); return true;
                case TypeCode.Boolean: instance = node.AsBool(); return true;
                case TypeCode.Char: instance = node.AsChar(); return true;
                case TypeCode.String: instance = node.AsString(); return true;
                default: instance = default; return false;
            }
        }

        bool TryConverter<T>(Node node, TypeData type, out object instance)
        {
            foreach (var converter in Container.Get<T, IConverter>())
            {
                if (converter.Validate(type))
                {
                    var context = With(node, type);
                    var index = References.Count;
                    References.Add(default);
                    instance = converter.Instantiate(context);
                    References[index] = instance;
                    converter.Initialize(ref instance, context);
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
                if (converter.Validate(type))
                {
                    var context = With(node, type);
                    var index = References.Count;
                    References.Add(default);
                    instance = converter.Instantiate(context);
                    References[index] = instance;
                    converter.Initialize(ref instance, context);
                    References[index] = instance;
                    return true;
                }
            }

            instance = default;
            return false;
        }

        T Instantiate<T>(TypeData type)
        {
            if (typeof(T).IsValueType) return default;
            return Cast<T>(Instantiate(type));
        }

        object Initialize(object instance, Node node, TypeData type)
        {
            if (instance == null) return instance;

            var members = type.InstanceMembers;
            References.Add(instance);
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

            return instance;
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