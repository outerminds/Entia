using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Json.Converters;

namespace Entia.Json
{
    [Flags]
    public enum Features
    {
        None = 0,
        All = ~0,
        Reference = 1 << 0,
        Abstract = 1 << 1,
    }

    public static class FeaturesExtensions
    {
        public static bool HasAll(this Features features, Features others) => (features & others) == others;
        public static bool HasAny(this Features features, Features others) => (features & others) != 0;
        public static bool HasNone(this Features features, Features others) => !features.HasAny(others);
    }

    public readonly struct ConvertToContext
    {
        public readonly object Instance;
        public readonly TypeData Type;
        public readonly Features Features;
        public readonly Dictionary<object, uint> References;
        public readonly Container Container;

        public ConvertToContext(Features features, Container container)
            : this(null, null, features, new Dictionary<object, uint>(), container) { }
        ConvertToContext(object instance, TypeData type, Features features, Dictionary<object, uint> references, Container container)
        {
            Instance = instance;
            Type = type;
            Features = features;
            References = references;
            Container = container;
        }

        public Node Convert<T>(in T instance) => Convert(instance, TypeUtility.GetData<T>());
        public Node Convert(object instance, Type type) => Convert(instance, TypeUtility.GetData(type));
        public Node Convert<T>(in T instance, TypeData type) =>
            TrySpecial(instance, type, out var node) ? node : Abstract<T>(instance, type);
        public Node Convert(object instance, TypeData type) =>
            TrySpecial(instance, type, out var node) ? node : Abstract(instance, type);

        public ConvertToContext With(object instance, TypeData type, Features? features = null) =>
            new ConvertToContext(instance, type, features ?? Features, References, Container);

        bool TrySpecial(object instance, TypeData type, out Node node)
        {
            if (instance is null)
            {
                node = Node.Null;
                return true;
            }

            switch (type.Code)
            {
                case TypeCode.Byte: node = (byte)instance; return true;
                case TypeCode.SByte: node = (sbyte)instance; return true;
                case TypeCode.Int16: node = (short)instance; return true;
                case TypeCode.Int32: node = (int)instance; return true;
                case TypeCode.Int64: node = (long)instance; return true;
                case TypeCode.UInt16: node = (ushort)instance; return true;
                case TypeCode.UInt32: node = (uint)instance; return true;
                case TypeCode.UInt64: node = (ulong)instance; return true;
                case TypeCode.Single: node = (float)instance; return true;
                case TypeCode.Double: node = (double)instance; return true;
                case TypeCode.Decimal: node = (decimal)instance; return true;
                case TypeCode.Boolean: node = (bool)instance; return true;
                case TypeCode.Char: node = (char)instance; return true;
                case TypeCode.String: node = (string)instance; return true;
                case TypeCode.Object when type.Definition == typeof(Nullable<>):
                    node = Convert(instance, type.Element);
                    return true;
                default:
                    if (References.TryGetValue(instance, out var reference))
                    {
                        node = Features.HasAll(Features.Reference) ? Node.Reference(reference) : Node.Null;
                        return true;
                    }
                    else if (instance is Type value)
                    {
                        node = Node.Type(value);
                        References[value] = node.Identifier;
                        return true;
                    }
                    else
                    {
                        node = default;
                        return false;
                    }
            }
        }

        Node Abstract<T>(object instance, TypeData type)
        {
            var concrete = instance.GetType();
            if (type.Type == concrete)
                return Concrete<T>(instance, type);
            else if (Features.HasAll(Features.Abstract))
                return Node.Abstract(concrete, Convert(instance, concrete));
            else
                return Node.Null;
        }

        Node Abstract(object instance, TypeData type)
        {
            var concrete = instance.GetType();
            if (type.Type == concrete)
                return Concrete(instance, type);
            else if (Features.HasAll(Features.Abstract))
                return Node.Abstract(concrete, Convert(instance, concrete));
            else
                return Node.Null;
        }

        Node Concrete<T>(object instance, TypeData type) =>
            Container.TryGet<T, IConverter>(out var converter) ?
            ConvertWith(instance, type, converter) : Default(instance, type);

        Node Concrete(object instance, TypeData type) =>
            Container.TryGet<IConverter>(type, out var converter) ?
            ConvertWith(instance, type, converter) : Default(instance, type);

        Node ConvertWith(object instance, TypeData type, IConverter converter)
        {
            var identifier = Reserve(instance);
            return converter.Convert(With(instance, type)).With(identifier);
        }

        Node Default(object instance, TypeData type)
        {
            var identifier = Reserve(instance);
            var fields = type.InstanceFields;
            var members = new Node[fields.Length * 2];
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                members[i * 2] = Node.String(field.Name, Node.Tags.Plain);
                members[i * 2 + 1] = Convert(field.GetValue(instance), field.FieldType);
            }
            return Node.Object(members).With(identifier);
        }

        uint Reserve(object instance) => References[instance] = Node.Reserve();
    }
}