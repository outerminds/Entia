using System;
using System.Collections.Generic;
using Entia.Json.Converters;

namespace Entia.Json
{
    public readonly struct FromContext
    {
        // Do not use a 'hard' cast '(T)value' because in some cases, a non-null value
        // may be of the wrong type, for example in the case of type name collision
        static T Cast<T>(object value) => value is T casted ? casted : default;

        public readonly Node Node;
        public readonly Type Type;
        public readonly Settings Settings;
        public readonly Dictionary<uint, object> References;

        public FromContext(Settings settings, Dictionary<uint, object> references)
            : this(null, null, settings, references) { }
        FromContext(Node node, Type type, Settings settings, Dictionary<uint, object> references)
        {
            Node = node;
            Type = type;
            Settings = settings;
            References = references;
        }

        public T Convert<T>(Node node, IConverter @default = null, IConverter @override = null) =>
            Cast<T>(Convert(node, typeof(T), @default ?? Converter.Default<T>(), @override));

        public object Convert(Node node, Type type, IConverter @default = null, IConverter @override = null)
        {
            switch (node.Kind)
            {
                case Node.Kinds.Null: return null;
                case Node.Kinds.Type: return node.AsType();
                case Node.Kinds.Reference: return GetReference(node);
                case Node.Kinds.Abstract:
                    return
                        Settings.Features.HasAll(Features.Abstract) &&
                        node.TryAbstract(out var concrete, out var value) ?
                        Convert(value, concrete) : null;
            }

            // Enums must be checked before primitives otherwise the cast to an 'TEnum?' fails.
            if (type.IsEnum) return node.AsEnum(type);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte: return node.AsByte();
                case TypeCode.SByte: return node.AsSByte();
                case TypeCode.Int16: return node.AsShort();
                case TypeCode.Int32: return node.AsInt();
                case TypeCode.Int64: return node.AsLong();
                case TypeCode.UInt16: return node.AsUShort();
                case TypeCode.UInt32: return node.AsUInt();
                case TypeCode.UInt64: return node.AsULong();
                case TypeCode.Single: return node.AsFloat();
                case TypeCode.Double: return node.AsDouble();
                case TypeCode.Decimal: return node.AsDecimal();
                case TypeCode.Boolean: return node.AsBool();
                case TypeCode.Char: return node.AsChar();
                case TypeCode.String: return node.AsString();
                case TypeCode.DBNull: return DBNull.Value;
            }

            var converter = Settings.Converter(type, @default, @override);
            var context = With(node, type);
            var instance = converter.Instantiate(context);
            UpdateReference(node, instance);
            // Do not initialize a null instance.
            if (instance is null) return null;
            converter.Initialize(ref instance, context);
            // Must be set again in the case where the boxed instance is unboxed and reboxed
            if (type.IsValueType) UpdateReference(node, instance);
            return instance;
        }

        public FromContext With(Node node = null, Type type = null) =>
            new FromContext(node ?? Node, type ?? Type, Settings, References);

        object GetReference(Node node) =>
            Settings.Features.HasAll(Features.Reference) &&
            References.TryGetValue(node.AsReference(), out var instance) ?
            instance : null;

        void UpdateReference(Node node, object instance)
        {
            if (Settings.Features.HasAll(Features.Reference))
                References[node.Identifier] = instance;
        }
    }
}