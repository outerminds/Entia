using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Json.Converters;

namespace Entia.Json
{
    public readonly struct ConvertToContext
    {
        public readonly object Instance;
        public readonly TypeData Type;
        public readonly Settings Settings;
        public readonly Dictionary<object, uint> References;

        public ConvertToContext(Settings settings) :
            this(null, null, settings, new Dictionary<object, uint>())
        { }

        ConvertToContext(object instance, TypeData type, Settings settings, Dictionary<object, uint> references)
        {
            Instance = instance;
            Type = type;
            Settings = settings;
            References = references;
        }

        public Node Convert<T>(in T instance) => Convert(instance, TypeUtility.GetData<T>());
        public Node Convert(object instance, Type type) => Convert(instance, TypeUtility.GetData(type));
        public Node Convert<T>(in T instance, TypeData type) =>
            TrySpecial(instance, type, out var node) ? node : Abstract<T>(instance, type);
        public Node Convert(object instance, TypeData type) =>
            TrySpecial(instance, type, out var node) ? node : Abstract(instance, type);

        public ConvertToContext With(object instance, TypeData type) =>
            new ConvertToContext(instance, type, Settings, References);

        bool TrySpecial(object instance, TypeData type, out Node node)
        {
            if (instance == null)
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
                default:
                    if (type.Definition == typeof(Nullable<>))
                    {
                        node = Convert(instance, type.Element);
                        return true;
                    }
                    else if (TryReference(instance, out var identifier))
                    {
                        node = Node.Reference(identifier);
                        return true;
                    }
                    else if (instance is Type value)
                    {
                        node = Node.Type(value);
                        UpdateReference(value, node.Identifier);
                        return true;
                    }
                    else
                    {
                        node = default;
                        return false;
                    }
            }
        }

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

        Node Abstract<T>(object instance, TypeData type)
        {
            var concrete = instance.GetType();
            if (type.Type == concrete)
                return Concrete<T>(instance, type);
            else if (Settings.Features.HasAll(Features.Abstract))
                return Node.Abstract(concrete, Convert(instance, concrete));
            else
                return Node.Null;
        }

        Node Abstract(object instance, TypeData type)
        {
            var concrete = instance.GetType();
            if (type.Type == concrete)
                return Concrete(instance, type);
            else if (Settings.Features.HasAll(Features.Abstract))
                return Node.Abstract(concrete, Convert(instance, concrete));
            else
                return Node.Null;
        }

        Node Concrete<T>(object instance, TypeData type) =>
            Settings.Container.TryGet<T, IConverter>(out var converter) ?
            ConvertWith(instance, type, converter) : Default(instance, type);

        Node Concrete(object instance, TypeData type) =>
            Settings.Container.TryGet<IConverter>(type, out var converter) ?
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

        uint Reserve(object instance)
        {
            var identifier = Node.Reserve();
            UpdateReference(instance, identifier);
            return identifier;
        }
    }
}