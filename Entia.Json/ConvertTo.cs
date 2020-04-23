using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Json.Converters;

namespace Entia.Json
{
    [Flags]
    public enum ConvertOptions
    {
        None = 0,
        All = ~0,
        Reference = 1 << 0,
        Abstract = 1 << 1,
    }

    public static class OptionsExtensions
    {
        public static bool HasAll(this ConvertOptions options, ConvertOptions others) => (options & others) == others;
        public static bool HasAny(this ConvertOptions options, ConvertOptions others) => (options & others) != 0;
        public static bool HasNone(this ConvertOptions options, ConvertOptions others) => !options.HasAny(others);
    }

    public readonly struct ConvertToContext
    {
        public readonly object Instance;
        public readonly TypeData Type;
        public readonly ConvertOptions Options;
        public readonly Dictionary<object, int> References;
        public readonly Container Container;

        public ConvertToContext(ConvertOptions options, Dictionary<object, int> references, Container container = null)
            : this(null, null, options, references, container) { }
        public ConvertToContext(object instance, TypeData type, ConvertOptions options, Dictionary<object, int> references, Container container = null)
        {
            Instance = instance;
            Type = type;
            Options = options;
            References = references;
            Container = container ?? new Container();
        }

        public Node Convert<T>(in T instance) =>
            TrySpecial(instance, typeof(T), out var special) ? special : Concrete<T>(instance);
        public Node Convert(object instance, Type type) =>
            TrySpecial(instance, type, out var special) ? special : Concrete(instance, type);
        public Node Convert(object instance, TypeData type) =>
            TrySpecial(instance, type, out var special) ? special : Concrete(instance, type);

        public ConvertToContext With(object instance, TypeData type, ConvertOptions? options = null) =>
            new ConvertToContext(instance, type, options ?? Options, References, Container);

        Node Concrete<T>(in T instance)
        {
            var data = TypeUtility.GetData<T>();
            if (TryPrimitive(instance, data, out var primitive)) return primitive;
            References[instance] = References.Count;
            if (TryConverter<T>(instance, data, out var node)) return node;
            return Default(instance, data);
        }

        Node Concrete(object instance, TypeData type)
        {
            if (TryPrimitive(instance, type, out var primitive)) return primitive;
            References[instance] = References.Count;
            if (TryConverter(instance, type, out var node)) return node;
            return Default(instance, type);
        }

        bool TryPrimitive(object instance, TypeData type, out Node node)
        {
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
                default: node = default; return false;
            }
        }

        bool TrySpecial(object instance, Type type, out Node node)
        {
            if (instance is null)
            {
                node = Node.Null;
                return true;
            }
            else if (Options.HasAll(ConvertOptions.Reference) && References.TryGetValue(instance, out var reference))
            {
                node = Node.Reference(reference);
                return true;
            }

            var concrete = instance.GetType();
            if (type != typeof(Type) && type != concrete)
            {
                node = Options.HasAll(ConvertOptions.Abstract) ?
                    Node.Abstract(Convert(concrete), Concrete(instance, concrete)) :
                    Node.Null;
                return true;
            }

            node = default;
            return false;
        }

        bool TryConverter<T>(object instance, TypeData type, out Node node)
        {
            foreach (var converter in Container.Get<T, IConverter>())
            {
                if (converter.Validate(type))
                {
                    node = converter.Convert(With(instance, type));
                    return true;
                }
            }

            node = default;
            return false;
        }

        bool TryConverter(object instance, TypeData type, out Node node)
        {
            foreach (var converter in Container.Get<IConverter>(type))
            {
                if (converter.Validate(type))
                {
                    node = converter.Convert(With(instance, type));
                    return true;
                }
            }

            node = default;
            return false;
        }

        Node Default(object instance, TypeData type)
        {
            var fields = type.InstanceFields;
            var members = new Node[fields.Length * 2];
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                members[i * 2] = field.Name;
                members[i * 2 + 1] = Convert(field.GetValue(instance), field.FieldType);
            }
            return Node.Object(members);
        }
    }

    public static partial class Serialization
    {
        public static Node Convert<T>(in T instance, ConvertOptions options = ConvertOptions.All, Container container = null, params object[] references) =>
            ToContext(options, references, container).Convert(instance);
        public static Node Convert(object instance, Type type, ConvertOptions options = ConvertOptions.All, Container container = null, params object[] references) =>
            ToContext(options, references, container).Convert(instance, type);

        static ConvertToContext ToContext(ConvertOptions options, object[] references, Container container)
        {
            var dictionary = new Dictionary<object, int>(references.Length);
            for (int i = 0; i < references.Length; i++) dictionary[references[i]] = dictionary.Count;
            return new ConvertToContext(options, dictionary, container);
        }
    }
}