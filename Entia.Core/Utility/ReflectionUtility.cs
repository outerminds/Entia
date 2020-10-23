using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

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
        public static bool TryGetGuid(Type type, out Guid guid)
        {
            guid = type.GUID;
            return type.HasGuid();
        }

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

            return string.Join(".", type.Declaring().Reverse().Select(Trimmed).Append(name));
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
            else if (definition) return type.GenericDefinition() == other;
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
            yield return type;
            foreach (var @base in type.Bases()) yield return @base;
            foreach (var @interface in type.GetInterfaces()) yield return @interface;
        }

        public static IEnumerable<Type> Bases(this Type type)
        {
            type = type.BaseType;
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        public static IEnumerable<Type> Declaring(this Type type)
        {
            type = type.DeclaringType;
            while (type != null)
            {
                yield return type;
                type = type.DeclaringType;
            }
        }

        public static Option<Type> GenericDefinition(this Type type) =>
            type.IsGenericType ? type.GetGenericTypeDefinition() : default;

        public static Option<object> DefaultInstance(this Type type)
        {
            try { return Array.CreateInstance(type, 1).GetValue(0); }
            catch { return default; }
        }

        public static Option<Type> DictionaryInterface(this Type type, bool generic) => generic ?
            type.GetInterfaces().FirstOrNone(@interface => @interface.GenericDefinition() == typeof(IDictionary<,>)) :
            type.GetInterfaces().FirstOrNone(@interface => @interface == typeof(IDictionary));

        public static Option<(Type key, Type value)> DictionaryArguments(this Type type, bool generic) => generic ?
            type.DictionaryInterface(true).Bind(enumerable => enumerable.GetGenericArguments().Two()) :
            type.DictionaryInterface(false).Return((typeof(object), typeof(object)));

        public static Option<Type> EnumerableInterface(this Type type, bool generic) => generic ?
            type.GetInterfaces().FirstOrNone(@interface => @interface.GenericDefinition() == typeof(IEnumerable<>)) :
            type.GetInterfaces().FirstOrNone(@interface => @interface == typeof(IEnumerable));

        public static Option<Type> EnumerableArgument(this Type type, bool generic) => generic ?
            type.EnumerableInterface(generic).Bind(enumerable => enumerable.GetGenericArguments().FirstOrNone()) :
            type.EnumerableInterface(generic).Return(typeof(object));

        public static Option<ConstructorInfo> EnumerableConstructor(this Type type, bool generic) =>
            type.EnumerableArgument(generic)
                .Bind(argument => argument.ArrayType())
                .Bind(array => type.Constructors(true, false).FirstOrNone(constructor =>
                    constructor.GetParameters().TryFirst(out var parameter) &&
                    array.Is(parameter.ParameterType)));

        public static Option<Type> ArrayType(this Type type) => Option.Try(() => type.MakeArrayType());

        public static Option<ConstructorInfo> SerializableConstructor(this Type type) =>
            type.Is<ISerializable>() ?
            type.Constructors(true, false).FirstOrNone(constructor =>
                constructor.GetParameters() is var parameters &&
                parameters.Length == 2 &&
                parameters[0].ParameterType == typeof(SerializationInfo) &&
                parameters[1].ParameterType == typeof(StreamingContext)) :
            Option.None();

        public static IEnumerable<MemberInfo> Members(this Type type, bool instance = true, bool @static = true)
        {
            var flags = default(BindingFlags);
            if (instance) flags |= Instance;
            if (@static) flags |= Static;
            if (flags == default) return Array.Empty<MemberInfo>();
            return type.Hierarchy().SelectMany(@base => @base.GetMembers(flags));
        }

        public static IEnumerable<TypeInfo> Types(this Type type, bool instance = true, bool @static = true) =>
            type.Members(instance, @static).OfType<TypeInfo>();

        public static IEnumerable<FieldInfo> Fields(this Type type, bool instance = true, bool @static = true) =>
            type.Members(instance, @static).OfType<FieldInfo>();

        public static IEnumerable<PropertyInfo> Properties(this Type type, bool instance = true, bool @static = true) =>
            type.Members(instance, @static).OfType<PropertyInfo>();

        public static IEnumerable<MethodInfo> Methods(this Type type, bool instance = true, bool @static = true) =>
            type.Members(instance, @static).OfType<MethodInfo>();

        public static IEnumerable<EventInfo> Events(this Type type, bool instance = true, bool @static = true) =>
            type.Members(instance, @static).OfType<EventInfo>();

        public static IEnumerable<ConstructorInfo> Constructors(this Type type, bool instance = true, bool @static = true)
        {
            var flags = default(BindingFlags);
            if (instance) flags |= Instance;
            if (@static) flags |= Static;
            if (flags == default) return Array.Empty<ConstructorInfo>();
            return type.GetConstructors(flags);
        }

        public static Option<ConstructorInfo> DefaultConstructor(this Type type) =>
            type.Constructors(true, false).FirstOrNone(constructor => constructor.GetParameters().None());

        public static Option<PropertyInfo> AutoProperty(this FieldInfo field)
        {
            if (field.IsPrivate && field.Name[0] == '<' &&
                field.Name.IndexOf('>') is var index && index > 0 &&
                field.Name.Substring(1, index - 1) is var name &&
                field.DeclaringType.GetProperty(name, All) is PropertyInfo property &&
                field.FieldType == property.PropertyType)
                return property;
            return Option.None();
        }

        public static Option<FieldInfo> BackingField(this PropertyInfo property) =>
            property.DeclaringType.Fields().FirstOrNone(field => field.AutoProperty() == property);

        public static bool IsAbstract(this PropertyInfo property) =>
            (property.GetMethod?.IsAbstract ?? false) || (property.SetMethod?.IsAbstract ?? false);
        public static bool IsConcrete(this PropertyInfo property) => !property.IsAbstract();
    }
}
