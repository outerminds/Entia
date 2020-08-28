﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Entia.Core
{
    public static class ReflectionUtility
    {
        static class Cache<T>
        {
            public static readonly TypeData Data = GetData(typeof(T));
        }

        public const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        public const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public const BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
        public const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        public const BindingFlags All = Instance | Static;

        public static ICollection<Assembly> AllAssemblies => _assemblies.Values;
        public static ICollection<Type> AllTypes => _types.Values;

        static readonly ConcurrentDictionary<string, Assembly> _assemblies = new ConcurrentDictionary<string, Assembly>();
        static readonly ConcurrentDictionary<string, Type> _types = new ConcurrentDictionary<string, Type>();
        static readonly ConcurrentDictionary<MemberInfo, IMemberData> _memberToData = new ConcurrentDictionary<MemberInfo, IMemberData>();
        static readonly ConcurrentDictionary<Guid, Type> _guidToType = new ConcurrentDictionary<Guid, Type>();

        static ReflectionUtility()
        {
            static void Register(Assembly assembly)
            {
                try
                {
                    var name = assembly.GetName();
                    _assemblies.TryAdd(name.Name, assembly);
                    _assemblies.TryAdd(name.FullName, assembly);

                    foreach (var type in assembly.GetTypes())
                    {
                        _types.TryAdd(type.Name, type);
                        _types.TryAdd(type.FullName, type);
                        _types.TryAdd(type.AssemblyQualifiedName, type);
                        if (type.HasGuid()) _guidToType.TryAdd(type.GUID, type);
                    }
                }
                catch { }
            }
            AppDomain.CurrentDomain.AssemblyLoad += (_, arguments) => Register(arguments.LoadedAssembly);
            AppDomain.CurrentDomain.GetAssemblies().Iterate(Register);
        }

        public static TypeData GetData<T>() => Cache<T>.Data;
        public static TypeData GetData(this Type type) => GetData((MemberInfo)type) as TypeData;
        public static FieldData GetData(this FieldInfo field) => GetData((MemberInfo)field) as FieldData;
        public static PropertyData GetData(this PropertyInfo property) => GetData((MemberInfo)property) as PropertyData;
        public static MethodData GetData(this MethodInfo method) => GetData((MemberInfo)method) as MethodData;
        public static ConstructorData GetData(this ConstructorInfo constructor) => GetData((MemberInfo)constructor) as ConstructorData;
        public static IMemberData GetData(this MemberInfo member) =>
            member is null ? null : _memberToData.GetOrAdd(member, key => key switch
            {
                Type type => new TypeData(type),
                FieldInfo field => new FieldData(field),
                PropertyInfo property => new PropertyData(property),
                ConstructorInfo constructor => new ConstructorData(constructor),
                MethodInfo method => new MethodData(method),
                _ => new MemberData(member),
            });


        public static bool TryGetAssembly(string name, out Assembly assembly) => _assemblies.TryGetValue(name, out assembly);
        public static bool TryGetType(string name, out Type type) => _types.TryGetValue(name, out type);
        public static bool TryGetType(Guid guid, out Type type) => _guidToType.TryGetValue(guid, out type);
        public static bool TryGetGuid(Type type, out Guid guid) => type.GetData().Guid.TryValue(out guid);

        public static bool HasGuid(this Type type) => type.IsDefined(typeof(GuidAttribute));

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

            return string.Join(".", GetData(type).Declaring
                .Reverse()
                .Select(declaring => Trimmed(declaring.Type))
                .Append(name));
        }

        public static string FullFormat(this Type type) =>
            type.DeclaringType is Type ? $"{type.DeclaringType.FullFormat()}.{type.Format()}" :
            type.Namespace is string ? $"{type.Namespace}.{type.Format()}" :
            type.Format();

        public static bool IsNullable(this Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static bool Is(this Type type, Type other, bool hierarchy = false, bool definition = false)
        {
            if (type == other) return true;
            else if (hierarchy) return type.Hierarchy().Any(child => child.Is(other, false, definition));
            else if (other.IsAssignableFrom(type)) return true;
            else if (definition) return type.IsGenericType && other.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == other;
            else return false;
        }

        public static bool Is(object value, Type type, bool hierarchy = false, bool definition = false) => value switch
        {
            null => type.IsClass,
            Type current => current.Is(type, hierarchy, definition),
            _ => value.GetType().Is(type, hierarchy, definition),
        };

        public static bool Is<T>(object value) => value switch
        {
            Type type => type.Is<T>(),
            _ => value is T,
        };

        public static bool Is<T>(this Type type) => typeof(T).IsAssignableFrom(type);

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
    }
}
