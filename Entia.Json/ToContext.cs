using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Json.Converters;

namespace Entia.Json
{
    public readonly struct ToContext
    {
        public readonly object Instance;
        public readonly Type Type;
        public readonly Settings Settings;
        public readonly Dictionary<object, uint> References;

        public ToContext(Settings settings) :
            this(null, null, settings, new Dictionary<object, uint>())
        { }

        ToContext(object instance, Type type, Settings settings, Dictionary<object, uint> references)
        {
            Instance = instance;
            Type = type;
            Settings = settings;
            References = references;
        }

        public Node Convert<T>(in T instance, IConverter @default = null, IConverter @override = null) =>
            Convert(instance, typeof(T), @default ?? Converter.Default<T>(), @override);

        public Node Convert(object instance, Type type, IConverter @default = null, IConverter @override = null)
        {
            if (instance is null || type.IsPointer) return Node.Null;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte: return (byte)instance;
                case TypeCode.SByte: return (sbyte)instance;
                case TypeCode.Int16: return (short)instance;
                case TypeCode.Int32: return (int)instance;
                case TypeCode.Int64: return (long)instance;
                case TypeCode.UInt16: return (ushort)instance;
                case TypeCode.UInt32: return (uint)instance;
                case TypeCode.UInt64: return (ulong)instance;
                case TypeCode.Single: return (float)instance;
                case TypeCode.Double: return (double)instance;
                case TypeCode.Decimal: return (decimal)instance;
                case TypeCode.Boolean: return (bool)instance;
                case TypeCode.Char: return (char)instance;
                case TypeCode.String: return (string)instance;
                case TypeCode.DBNull: return Node.EmptyObject;
            }

            if (TryReference(instance, out var reference))
                return Node.Reference(reference);

            var concrete = instance.GetType();
            if (type == concrete)
            {
                var converter = Settings.Converter(concrete, @default, @override);
                var identifier = Reserve(instance);
                return converter.Convert(With(instance, concrete)).With(identifier);
            }
            else if (instance is Type)
            {
                var converter = Settings.Converter<Type>(null, @override);
                var identifier = Reserve(instance);
                return converter.Convert(With(instance, concrete)).With(identifier);
            }
            else if (type.GenericDefinition() == typeof(Nullable<>))
                return Convert(instance, instance.GetType(), @default);
            else if (Settings.Features.HasAll(Features.Abstract))
                return Node.Abstract(concrete, Convert(instance, concrete));
            else
                return Node.Null;
        }

        public ToContext With(object instance, Type type) =>
            new ToContext(instance, type, Settings, References);

        bool TryReference(object instance, out uint identifier)
        {
            if (Settings.Features.HasAll(Features.Reference))
                return References.TryGetValue(instance, out identifier);
            identifier = default;
            return false;
        }

        void UpdateReference(object instance, uint identifier)
        {
            if (Settings.Features.HasAll(Features.Reference))
                References[instance] = identifier;
        }

        uint Reserve(object instance)
        {
            var identifier = Node.Reserve();
            UpdateReference(instance, identifier);
            return identifier;
        }
    }
}