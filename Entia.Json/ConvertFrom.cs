using System;
using System.Collections.Generic;
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
        public readonly Settings Settings;
        public readonly Dictionary<uint, object> References;

        public ConvertFromContext(Settings settings, Dictionary<uint, object> references)
            : this(null, null, settings, references) { }
        ConvertFromContext(Node node, TypeData type, Settings settings, Dictionary<uint, object> references)
        {
            Node = node;
            Type = type;
            Settings = settings;
            References = references;
        }

        public T Convert<T>(Node node) => Convert<T>(node, ReflectionUtility.GetData<T>());
        public object Convert(Node node, Type type) => Convert(node, ReflectionUtility.GetData(type));
        public T Convert<T>(Node node, TypeData type) =>
            TrySpecial(node, type, out var instance) ? Cast<T>(instance) :
            TryConverter<T>(node, type, out instance) ? Cast<T>(instance) :
            Default<T>(node, type);
        public object Convert(Node node, TypeData type) =>
            TrySpecial(node, type, out var instance) ? instance :
            TryConverter(node, type, out instance) ? instance :
            Default(node, type);

        public T Instantiate<T>() => Instantiate<T>(ReflectionUtility.GetData<T>());

        public object Instantiate(Type type) => Instantiate(ReflectionUtility.GetData(type));

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
                type.DefaultConstructor
                    .Map(constructor => constructor.Constructor.Invoke(Array.Empty<object>()))
                    .Or(type, state => FormatterServices.GetUninitializedObject(state));
        }

        public ConvertFromContext With(Node node = null, TypeData type = null) =>
            new ConvertFromContext(node ?? Node, type ?? Type, Settings, References);

        bool TrySpecial(Node node, TypeData type, out object instance)
        {
            switch (node.Kind)
            {
                case Node.Kinds.Null: instance = null; return true;
                case Node.Kinds.Type: instance = node.AsType(); return true;
                case Node.Kinds.Reference: instance = GetReference(node); return true;
                case Node.Kinds.Abstract:
                    instance =
                        Settings.Features.HasAll(Features.Abstract) &&
                        node.TryAbstract(out var concrete, out var value) ?
                        Convert(value, concrete) : null;
                    return true;
                default:
                    if (type.Type.IsEnum)
                    {
                        instance = node.AsEnum(type);
                        return true;
                    }
                    else if (type.Definition == typeof(Nullable<>) && type.Element.TryValue(out var element))
                    {
                        instance = Convert(node, element);
                        return true;
                    }

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
        }

        object GetReference(Node node) =>
            Settings.Features.HasAll(Features.Reference) &&
            References.TryGetValue(node.AsReference(), out var instance) ?
            instance : null;

        void UpdateReference(Node node, object instance)
        {
            if (Settings.Features.HasAll(Features.Reference))
                References[node.Identifier] = instance;
        }

        bool TryConverter<T>(Node node, TypeData type, out object instance)
        {
            if (Settings.Container.TryGet<T, IConverter>(out var converter))
            {
                instance = Instantiate(node, type, converter);
                return true;
            }

            instance = default;
            return false;
        }

        bool TryConverter(Node node, TypeData type, out object instance)
        {
            if (Settings.Container.TryGet<IConverter>(type, out var converter))
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
            var instance = converter.Instantiate(context);
            UpdateReference(node, instance);
            converter.Initialize(ref instance, context);
            // NOTE: must be set again in the case where the boxed instance is unboxed and reboxed
            if (type.Type.IsValueType) UpdateReference(node, instance);
            return instance;
        }

        T Default<T>(Node node, TypeData type)
        {
            // NOTE: instance must be boxed to ensure it is not copied around if it is a struct
            var instance = (object)Instantiate<T>(type);
            UpdateReference(node, instance);
            Initialize(instance, node, type);
            return Cast<T>(instance);
        }

        object Default(Node node, TypeData type)
        {
            var instance = Instantiate(type);
            UpdateReference(node, instance);
            Initialize(instance, node, type);
            return instance;
        }

        void Initialize(object instance, Node node, TypeData type)
        {
            if (instance == null) return;

            var fields = type.Fields;
            var properties = type.Properties;
            foreach (var (key, value) in node.Members())
            {
                if (fields.TryGetValue(key, out var field) && field.Field.IsInstance())
                    field.Field.SetValue(instance, Convert(value, field.Type));
                else if (properties.TryGetValue(key, out var property) && property.Property.IsInstance())
                {
                    if (property.BackingField.TryValue(out field))
                        field.Field.SetValue(instance, Convert(value, field.Type));
                    else
                        property.Property.SetValue(instance, Convert(value, property.Type));
                }
            }
        }
    }
}