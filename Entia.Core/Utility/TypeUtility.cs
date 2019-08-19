using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Entia.Core
{
    public sealed class TypeData
    {
        public static implicit operator TypeData(Type type) => TypeUtility.GetData(type);
        public static implicit operator Type(TypeData type) => type.Type;

        public readonly Type Type;
        public TypeCode Code => _code.Value;
        public Type Element => _element.Value;
        public Type Definition => _definition.Value;
        public Dictionary<int, MemberInfo> Members => _members.Value;
        public Dictionary<string, FieldInfo> Fields => _fields.Value;
        public Dictionary<string, PropertyInfo> Properties => _properties.Value;
        public Dictionary<string, MethodInfo> Methods => _methods.Value;
        public MemberInfo[] StaticMembers => _staticMembers.Value;
        public MethodInfo[] StaticMethods => _staticMethods.Value;
        public MemberInfo[] InstanceMembers => _instanceMembers.Value;
        public FieldInfo[] InstanceFields => _instanceFields.Value;
        public PropertyInfo[] InstanceProperties => _instanceProperties.Value;
        public MethodInfo[] InstanceMethods => _instanceMethods.Value;
        public ConstructorInfo[] InstanceConstructors => _instanceConstructors.Value;
        public Type[] Interfaces => _interfaces.Value;
        public Type[] Declaring => _declaring.Value;
        public Type[] Arguments => _arguments.Value;
        public Type[] Bases => _bases.Value;
        public bool IsShallow => _isShallow.Value;
        public bool IsPlain => _isPlain.Value;
        public bool IsBlittable => _isBlittable.Value;
        public bool IsCyclic => _isCyclic.Value;
        public object Default => _default.Value;
        public int? Size => _size.Value;

        readonly Lazy<TypeCode> _code;
        readonly Lazy<Type> _element;
        readonly Lazy<Type> _definition;
        readonly Lazy<Dictionary<int, MemberInfo>> _members;
        readonly Lazy<Dictionary<string, FieldInfo>> _fields;
        readonly Lazy<Dictionary<string, PropertyInfo>> _properties;
        readonly Lazy<Dictionary<string, MethodInfo>> _methods;
        readonly Lazy<MemberInfo[]> _staticMembers;
        readonly Lazy<MethodInfo[]> _staticMethods;
        readonly Lazy<MemberInfo[]> _instanceMembers;
        readonly Lazy<FieldInfo[]> _instanceFields;
        readonly Lazy<PropertyInfo[]> _instanceProperties;
        readonly Lazy<MethodInfo[]> _instanceMethods;
        readonly Lazy<ConstructorInfo[]> _instanceConstructors;
        readonly Lazy<Type[]> _interfaces;
        readonly Lazy<Type[]> _declaring;
        readonly Lazy<Type[]> _arguments;
        readonly Lazy<Type[]> _bases;
        readonly Lazy<bool> _isShallow;
        readonly Lazy<bool> _isPlain;
        readonly Lazy<bool> _isBlittable;
        readonly Lazy<bool> _isCyclic;
        readonly Lazy<object> _default;
        readonly Lazy<int?> _size;

        public TypeData(Type type)
        {
            MemberInfo[] GetMembers(Type current, Type[] bases) => bases.Prepend(current)
                .SelectMany(@base => @base.GetMembers(TypeUtility.Instance))
                .Distinct()
                .ToArray();

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
                try { return Array.CreateInstance(current, 1).GetValue(0); }
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

            bool GetIsShallow(Type current, FieldInfo[] fields)
            {
                if (current.IsArray && current.GetElementType() is Type element)
                    return GetIsPlain(element, element.InstanceFields());

                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(string) ||
                        GetIsPlain(field.FieldType, field.FieldType.InstanceFields())) continue;
                    return false;
                }
                return true;
            }

            bool GetIsPlain(Type current, FieldInfo[] fields)
            {
                if (current.IsPrimitive || current.IsPointer || current.IsEnum) return true;
                if (current.IsValueType)
                {
                    foreach (var field in fields)
                    {
                        if (GetIsPlain(field.FieldType, field.FieldType.InstanceFields())) continue;
                        else return false;
                    }
                    return true;
                }
                return false;
            }

            bool GetIsBlittable(Type current, FieldInfo[] fields)
            {
                if (current.IsPrimitive || current.IsPointer || current.IsEnum)
                    return current != typeof(bool) && current != typeof(char) && current != typeof(decimal);
                else if (current.IsGenericType) return false;
                else if (current.IsValueType)
                {
                    foreach (var field in fields)
                    {
                        if (GetIsBlittable(field.FieldType, field.FieldType.InstanceFields())) continue;
                        else return false;
                    }
                    return true;
                }
                else return false;
            }

            bool GetIsCyclic(Type current, FieldInfo[] fields, HashSet<Type> types)
            {
                if (current.IsPrimitive || current.IsEnum) return false;
                else if (types.Add(current))
                {
                    if (current.GetElementType() is Type element)
                        return GetIsCyclic(element, element.InstanceFields(), types);

                    foreach (var field in fields)
                    {
                        if (GetIsCyclic(field.FieldType, field.FieldType.InstanceFields(), new HashSet<Type>(types)))
                            return true;
                    }
                    return false;
                }
                else return true;
            }

            unsafe int? GetSize(Type current)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean: return sizeof(bool);
                    case TypeCode.Byte: return sizeof(byte);
                    case TypeCode.Char: return sizeof(char);
                    case TypeCode.DateTime: return sizeof(DateTime);
                    case TypeCode.Decimal: return sizeof(decimal);
                    case TypeCode.Double: return sizeof(double);
                    case TypeCode.Int16: return sizeof(short);
                    case TypeCode.Int32: return sizeof(int);
                    case TypeCode.Int64: return sizeof(long);
                    case TypeCode.SByte: return sizeof(sbyte);
                    case TypeCode.Single: return sizeof(float);
                    case TypeCode.UInt16: return sizeof(ushort);
                    case TypeCode.UInt32: return sizeof(uint);
                    case TypeCode.UInt64: return sizeof(ulong);
                    default:
                        // NOTE: do not 'try-catch' 'Marshal.SizeOf' because it may cause inconsistencies between
                        // serialization and deserialization if they occur on different platforms
                        if (IsBlittable) return Marshal.SizeOf(current);
                        return null;
                }
            }

            Type = type;
            _code = new Lazy<TypeCode>(() => Type.GetTypeCode(type));
            _interfaces = new Lazy<Type[]>(() => Type.GetInterfaces());
            _bases = new Lazy<Type[]>(() => GetBases(Type).ToArray());
            _element = new Lazy<Type>(() => GetElement(Type, Interfaces));
            _definition = new Lazy<Type>(() => Type.IsGenericType ? Type.GetGenericTypeDefinition() : default);
            _members = new Lazy<Dictionary<int, MemberInfo>>(() => Type.GetMembers(TypeUtility.All).ToDictionary(member => member.MetadataToken));
            _fields = new Lazy<Dictionary<string, FieldInfo>>(() => Members.Values.OfType<FieldInfo>().ToDictionary(member => member.Name));
            _properties = new Lazy<Dictionary<string, PropertyInfo>>(() => Members.Values.OfType<PropertyInfo>().ToDictionary(member => member.Name));
            _methods = new Lazy<Dictionary<string, MethodInfo>>(() => Members.Values.OfType<MethodInfo>().ToDictionary(member => member.Name));
            _staticMembers = new Lazy<MemberInfo[]>(() => Type.GetMembers(TypeUtility.Static));
            _staticMethods = new Lazy<MethodInfo[]>(() => StaticMembers.OfType<MethodInfo>().ToArray());
            _instanceMembers = new Lazy<MemberInfo[]>(() => GetMembers(Type, Bases));
            _instanceFields = new Lazy<FieldInfo[]>(() => InstanceMembers.OfType<FieldInfo>().ToArray());
            _instanceProperties = new Lazy<PropertyInfo[]>(() => InstanceMembers.OfType<PropertyInfo>().ToArray());
            _instanceMethods = new Lazy<MethodInfo[]>(() => InstanceMembers.OfType<MethodInfo>().ToArray());
            _instanceConstructors = new Lazy<ConstructorInfo[]>(() => InstanceMembers.OfType<ConstructorInfo>().ToArray());
            _declaring = new Lazy<Type[]>(() => GetDeclaring(Type).ToArray());
            _arguments = new Lazy<Type[]>(() => Type.GetGenericArguments());
            _isShallow = new Lazy<bool>(() => GetIsShallow(Type, InstanceFields));
            _isPlain = new Lazy<bool>(() => GetIsPlain(Type, InstanceFields));
            _isBlittable = new Lazy<bool>(() => GetIsBlittable(Type.IsArray ? Type.GetElementType() : Type, InstanceFields));
            _isCyclic = new Lazy<bool>(() => GetIsCyclic(Type, InstanceFields, new HashSet<Type>()));
            _default = new Lazy<object>(() => GetDefault(Type));
            _size = new Lazy<int?>(() => GetSize(Type));
        }

        public override string ToString() => Type.FullFormat();
    }

    public static class TypeUtility
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

        public static IEnumerable<Assembly> AllAssemblies =>
            AppDomain.CurrentDomain.GetAssemblies();
        public static IEnumerable<Type> AllTypes =>
            AllAssemblies.Select(assembly => Option.Try(() => assembly.GetTypes())).Choose().SelectMany(_ => _);

        static readonly ConcurrentDictionary<Type, TypeData> _typeToData = new ConcurrentDictionary<Type, TypeData>();

        public static TypeData GetData<T>() => Cache<T>.Data;

        public static TypeData GetData(Type type)
        {
            if (_typeToData.TryGetValue(type, out var data)) return data;
            _typeToData.TryAdd(type, data = new TypeData(type));
            return data;
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

        public static bool Is(this Type type, Type other, bool hierarchy = false, bool definition = false) =>
            type == other ||
            (hierarchy ? type.Hierarchy().Any(child => child.Is(other, false, definition)) : other.IsAssignableFrom(type)) ||
            (definition && type.IsGenericType && other.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == other);

        public static bool Is<T>(this Type type) => typeof(T).IsAssignableFrom(type);

        public static bool Is(object value, Type type, bool hierarchy = false, bool definition = false)
        {
            switch (value)
            {
                case null: return type.IsClass;
                case Type current: return current.Is(type, hierarchy, definition);
                default: return value.GetType().Is(type, hierarchy, definition);
            }
        }

        public static bool Is<T>(object value)
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

        public static bool IsBlittable(this Type type) => GetData(type).IsBlittable;
        public static bool IsBlittable(object value) => value is null || IsBlittable(value.GetType());
        public static bool IsPlain(this Type type) => GetData(type).IsPlain;
        public static bool IsPlain(object value) => value is null || IsPlain(value.GetType());
        public static bool IsDefault(object value) => value is null || value.Equals(GetData(value.GetType()).Default);
        public static bool IsPrimitive(object value) => value is null || value.GetType().IsPrimitive;
        public static bool IsCyclic(this Type type) => GetData(type).IsCyclic;
        public static int? Size(this Type type) => GetData(type).Size;
        public static MemberInfo Member(this Type type, int token) => GetData(type).Members.TryGetValue(token, out var member) ? member : default;
        public static FieldInfo Field(this Type type, int token) => type.Member(token) as FieldInfo;
        public static PropertyInfo Property(this Type type, int token) => type.Member(token) as PropertyInfo;
        public static MethodInfo Method(this Type type, int token) => type.Member(token) as MethodInfo;
        public static ConstructorInfo Constructor(this Type type, int token) => type.Member(token) as ConstructorInfo;
        public static MemberInfo[] StaticMembers(this Type type) => GetData(type).StaticMembers;
        public static MethodInfo[] StaticMethods(this Type type) => GetData(type).StaticMethods;
        public static MemberInfo[] InstanceMembers(this Type type) => GetData(type).InstanceMembers;
        public static FieldInfo[] InstanceFields(this Type type) => GetData(type).InstanceFields;
        public static PropertyInfo[] InstanceProperties(this Type type) => GetData(type).InstanceProperties;
        public static MethodInfo[] InstanceMethods(this Type type) => GetData(type).InstanceMethods;
        public static ConstructorInfo[] InstanceConstructors(this Type type) => GetData(type).InstanceConstructors;
        public static Type[] Bases(this Type type) => GetData(type).Bases;
        public static Type[] Interfaces(this Type type) => GetData(type).Interfaces;
        public static Type[] Declaring(this Type type) => GetData(type).Declaring;
        public static object Default(this Type type) => GetData(type).Default;
    }
}
