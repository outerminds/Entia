using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Core
{
    public static class TypeUtility
    {

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

        static readonly Concurrent<Dictionary<Type, object>> _typeToDefaults = new Dictionary<Type, object>();
        static readonly Concurrent<Dictionary<(Type, BindingFlags), FieldInfo[]>> _typeToFields = new Dictionary<(Type, BindingFlags), FieldInfo[]>();

        public static object GetDefault(Type type) => type.IsValueType ?
            _typeToDefaults.ReadValueOrWrite(type, type, key => (key, Activator.CreateInstance(key))) :
            null;

        public static FieldInfo[] GetFields(Type type, BindingFlags flags = Instance) =>
            _typeToFields.ReadValueOrWrite((type, flags), (type, flags), key => (key, key.type.GetFields(key.flags)));

        public static Result<object> GetValue(this object instance, string member, BindingFlags flags = Instance) =>
            instance.GetType().GetMember(member, flags)
                .Select(current =>
                {
                    switch (current)
                    {
                        case FieldInfo field: return Result.Try(field.GetValue, instance);
                        case PropertyInfo property: return Result.Try(property.GetValue, instance);
                        case MethodInfo method when method.GetParameters().Length == 0: return Result.Try(method.Invoke, instance, Type.EmptyTypes);
                        default: return Result.Failure();
                    }
                })
                .Any();

        public static Result<T> GetValue<T>(this object instance, string member, BindingFlags flags = Instance) => instance.GetValue(member, flags).Cast<T>();

        public static bool IsValueTuple(this Type type)
        {
            if (!type.IsGenericType) return false;

            var definition = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();
            return
                definition == typeof(ValueTuple<>) ||
                definition == typeof(ValueTuple<,>) ||
                definition == typeof(ValueTuple<,,>) ||
                definition == typeof(ValueTuple<,,,>) ||
                definition == typeof(ValueTuple<,,,,>) ||
                definition == typeof(ValueTuple<,,,,,>) ||
                definition == typeof(ValueTuple<,,,,,,>) ||
                definition == typeof(ValueTuple<,,,,,,,>);
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

            return string.Join(".", type.Outer().Reverse().Select(Trimmed).Append(name));
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

        public static bool Is(this object value, Type type, bool hierarchy = false, bool definition = false)
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

        public static IEnumerable<Type> Hierarchy(this Type type) => type.Bases().Prepend(type).Concat(type.GetInterfaces());

        public static IEnumerable<Type> Outer(this Type type)
        {
            var current = type.DeclaringType;
            while (current != null)
            {
                yield return current;
                current = current.DeclaringType;
            }
        }

        public static IEnumerable<Type> Bases(this Type type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static bool Validate(object value, Func<object, bool?> @base, Func<MemberInfo, object, bool> validate = null)
        {
            validate = validate ?? ((_, __) => true);
            var map = new Dictionary<object, bool>();
            bool Next(object current)
            {
                var result = @base(current);
                if (result.HasValue) return result.Value;
                if (map.TryGetValue(current, out var cached)) return cached;
                map[current] = true;

                switch (current)
                {
                    case Array array:
                        {
                            var type = array.GetType().GetElementType();
                            for (var i = 0; i < array.Length; i++)
                            {
                                var item = array.GetValue(i);
                                if (!validate(type, item) || !Next(item))
                                    return map[current] = false;
                            }
                            return true;
                        }
                    default:
                        foreach (var field in GetFields(current.GetType()))
                        {
                            var item = field.GetValue(current);
                            if (!validate(field, item) || !Next(item))
                                return map[current] = false;
                        }

                        return true;
                }
            }

            return Next(value);
        }

        public static bool IsShallow(this Type type, object value) =>
            IsImmutable(value) || IsDefault(value) || (type == value.GetType() && IsPlain(value));

        public static bool IsDefault(object value) => value is null || value.Equals(GetDefault(value.GetType()));

        public static bool IsPlain(object value) =>
            Validate(value,
                current => IsValue(current) ? true : current.GetType().IsValueType ? Nullable.Null<bool>() : false,
                (member, current) =>
                    member is FieldInfo field ? field.FieldType == current?.GetType() :
                    member is Type type ? type == current?.GetType() :
                    IsValue(current));

        public static bool IsImmutable(object value) =>
            Validate(value,
                current => IsValue(current) ? true : current is IList list && !list.IsReadOnly ? false : Nullable.Null<bool>(),
                (member, _) => member is FieldInfo field ? field.IsInitOnly : true);

        public static bool IsPrimitive(object value) => value is null || value is string || value.GetType().IsPrimitive;

        static bool IsValue(object value) => IsPrimitive(value) || (value is IList list && list.IsFixedSize && list.Count == 0);
    }
}
