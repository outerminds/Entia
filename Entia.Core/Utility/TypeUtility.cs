using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Core
{
    public sealed class TypeData
    {
        public readonly Type Type;
        public readonly Type Element;
        public readonly MemberInfo[] StaticMembers;
        public readonly MemberInfo[] InstanceMembers;
        public readonly FieldInfo[] InstanceFields;
        public readonly Type[] Interfaces;
        public readonly Type[] Declaring;
        public readonly Type[] Bases;
        public readonly bool IsPlain;
        public readonly object Default;

        public TypeData(Type type)
        {
            Type GetElement(Type current, Type[] interfaces)
            {
                if (current.IsArray) return type.GetElementType();
                return interfaces
                    .Where(child => child.IsGenericType && child.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .SelectMany(child => child.GetGenericArguments())
                    .FirstOrDefault();
            }

            object GetDefault(Type current)
            {
                try { return current.IsValueType ? Activator.CreateInstance(current) : null; }
                catch { return null; }
            }

            IEnumerable<Type> GetBases(Type current)
            {
                current = current.BaseType;
                while (current != null)
                {
                    yield return current;
                    current = current.BaseType;
                }
            }

            IEnumerable<Type> GetDeclaring(Type current)
            {
                current = current.DeclaringType;
                while (current != null)
                {
                    yield return current;
                    current = current.DeclaringType;
                }
            }

            bool GetIsPlain(Type current, FieldInfo[] fields)
            {
                if (current.IsPrimitive || current == typeof(string) || current.GetCustomAttributes(typeof(PlainAttribute)).Any()) return true;
                if (current.IsValueType)
                {
                    foreach (var field in fields)
                        if (!GetIsPlain(field.FieldType, field.FieldType.GetFields(TypeUtility.Instance))) return false;
                    return true;
                }
                return false;
            }

            Type = type;
            StaticMembers = Type.GetMembers(TypeUtility.Static);
            InstanceMembers = Type.GetMembers(TypeUtility.Instance);
            InstanceFields = Type.GetFields(TypeUtility.Instance);
            Interfaces = Type.GetInterfaces();
            Element = GetElement(Type, Interfaces);
            Bases = GetBases(Type).ToArray();
            Declaring = GetDeclaring(Type).ToArray();
            IsPlain = GetIsPlain(Type, InstanceFields);
            Default = GetDefault(Type);
        }
    }

    public static class TypeUtility
    {
        public static class Cache<T>
        {
            public static readonly TypeData Data = GetData(typeof(T));
        }

        public const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic;
        public const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        public const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public const BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
        public const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static IEnumerable<Assembly> AllAssemblies => AppDomain.CurrentDomain.GetAssemblies();
        public static IEnumerable<Type> AllTypes
        {
            get
            {
                foreach (var assembly in AllAssemblies)
                {
                    try { foreach (var type in assembly.GetTypes()) yield return type; }
                    finally { }
                }
            }
        }

        static readonly Concurrent<Dictionary<Type, TypeData>> _typeToData = new Dictionary<Type, TypeData>();

        public static TypeData GetData(Type type)
        {
            using (var read = _typeToData.Read(true))
            {
                if (read.Value.TryGetValue(type, out var value)) return value;
                var data = new TypeData(type);
                using (var write = _typeToData.Write())
                {
                    if (write.Value.TryGetValue(type, out value)) return value;
                    return write.Value[type] = data;
                }
            }
        }

        public static string Trimmed(this Type type) => type.Name.Split('`').First();

        public static string Format(this Type type)
        {
            var name = type.Name;
            if (type.IsGenericParameter) return name;

            if (type.IsGenericType)
            {
                var arguments = string.Join(", ", type.GetGenericArguments().Select(Format));
                name = $"{type.Trimmed()}<{arguments}>";
            }

            return string.Join(".", type.Declaring().Reverse().Select(Trimmed).Append(name));
        }

        public static string FullFormat(this Type type) =>
            type.DeclaringType is Type ? $"{type.DeclaringType.FullFormat()}.{type.Format()}" :
            type.Namespace is string ? $"{type.Namespace}.{type.Format()}" :
            type.Format();

        public static bool TryElement(this Type type, out Type element)
        {
            if (type.IsArray) element = type.GetElementType();
            else
            {
                element = type.GetInterfaces()
                    .Where(child => child.IsGenericType && child.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .SelectMany(child => child.GetGenericArguments())
                    .FirstOrDefault();
            }

            return element != null;
        }

        public static bool Is(object value, Type type, bool hierarchy = false, bool definition = false)
        {
            switch (value)
            {
                case null: return false;
                case Type current: return current.Is(type, hierarchy, definition);
                default: return value.GetType().Is(type, hierarchy, definition);
            }
        }

        public static bool Is(this Type type, Type other, bool hierarchy = false, bool definition = false) =>
            type == other ||
            (hierarchy ? type.Hierarchy().Any(child => child.Is(other, false, definition)) : other.IsAssignableFrom(type)) ||
            (definition && type.IsGenericType && other.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == other);

        public static bool Is<T>(this Type type) => typeof(T).IsAssignableFrom(type);

        public static bool Is<T>(this object value)
        {
            switch (value)
            {
                case Type type: return type.Is<T>();
                default: return value is T;
            }
        }

        public static IEnumerable<string> Path(this Type type)
        {
            var stack = new Stack<string>();
            var current = type;
            var root = type;
            while (current != null)
            {
                stack.Push(current.Name.Split('`').FirstOrDefault());
                root = current;
                current = current.DeclaringType;
            }

            return stack.Prepend(root.Namespace.Split('.'));
        }

        public static IEnumerable<Type> Hierarchy(this Type type)
        {
            var data = GetData(type);
            yield return type;
            foreach (var @base in data.Bases) yield return @base;
            foreach (var @interface in data.Interfaces) yield return @interface;
        }

        public static bool IsPlain(this Type type) => GetData(type).IsPlain;
        public static bool IsPlain(object value) => value is null || GetData(value.GetType()).IsPlain;
        public static bool IsDefault(object value) => value is null || value.Equals(GetData(value.GetType()).Default);
        public static bool IsPrimitive(object value) => value is null || value is string || value.GetType().IsPrimitive;
        public static MemberInfo[] InstanceMembers(this Type type) => GetData(type).InstanceMembers;
        public static MemberInfo[] StaticMembers(this Type type) => GetData(type).StaticMembers;
        public static FieldInfo[] InstanceFields(this Type type) => GetData(type).InstanceFields;
        public static Type[] Bases(this Type type) => GetData(type).Bases;
        public static Type[] Interfaces(this Type type) => GetData(type).Interfaces;
        public static Type[] Declaring(this Type type) => GetData(type).Declaring;
        public static object Default(this Type type) => GetData(type).Default;
    }
}
