using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;
using Entia.Modules.Serialization;
using Entia.Experimental.Serialization;

// TODO: writer serializers
// add a Lazy<IDescription> to constant
namespace Entia.Experiment.Serialization
{
    [Implementation(typeof(List<>), typeof(ListDescriptor<>))]
    [Implementation(typeof(List<>), typeof(ListDescriptor))]
    [Implementation(typeof(Dictionary<,>), typeof(DictionaryDescriptor<,>))]
    [Implementation(typeof(Dictionary<,>), typeof(DictionaryDescriptor))]
    public interface IDescriptor : ITrait,
        IImplementation<bool, ConstantDescriptor<bool>>,
        IImplementation<byte, ConstantDescriptor<byte>>,
        IImplementation<sbyte, ConstantDescriptor<sbyte>>,
        IImplementation<ushort, ConstantDescriptor<ushort>>,
        IImplementation<short, ConstantDescriptor<short>>,
        IImplementation<uint, ConstantDescriptor<uint>>,
        IImplementation<int, ConstantDescriptor<int>>,
        IImplementation<ulong, ConstantDescriptor<ulong>>,
        IImplementation<long, ConstantDescriptor<long>>,
        IImplementation<float, ConstantDescriptor<float>>,
        IImplementation<double, ConstantDescriptor<double>>,
        IImplementation<decimal, ConstantDescriptor<decimal>>,
        IImplementation<string, ConstantDescriptor<string>>,
        IImplementation<DateTime, ConstantDescriptor<DateTime>>,
        IImplementation<TimeSpan, ConstantDescriptor>,
        IImplementation<Unit, ConstantDescriptor>,

        IImplementation<Assembly, AssemblyDescriptor>,
        IImplementation<Module, ModuleDescriptor>,
        IImplementation<Type, TypeDescriptor>,
        IImplementation<MethodInfo, MethodDescriptor>,
        IImplementation<MemberInfo, MemberDescriptor>,

        IImplementation<Array, ArrayDescriptor>,
        IImplementation<object, ObjectDescriptor>
    {
        bool Describe(object instance, out IDescription description, in DescribeContext context);
        bool Instantiate(IDescription description, out object instance, in InstantiateContext context);
    }

    public abstract class Descriptor<T> : IDescriptor
    {
        public abstract bool Describe(in T instance, out IDescription description, in DescribeContext context);
        public abstract bool Instantiate(IDescription description, out T instance, in InstantiateContext context);

        bool IDescriptor.Describe(object instance, out IDescription description, in DescribeContext context)
        {
            if (instance is T casted) return Describe(casted, out description, context);
            description = default;
            return false;
        }

        bool IDescriptor.Instantiate(IDescription description, out object instance, in InstantiateContext context)
        {
            if (Instantiate(description, out var value, context))
            {
                instance = value;
                return true;
            }
            instance = default;
            return false;
        }
    }

    public readonly struct DescribeContext
    {
        public readonly TypeData Type;
        public readonly Dictionary<object, IDescription> References;
        public readonly World World;

        public DescribeContext(Dictionary<object, IDescription> references, World world) : this(null, references, world) { }
        public DescribeContext(TypeData type, Dictionary<object, IDescription> references, World world)
        {
            Type = type;
            References = references;
            World = world;
        }

        public bool Describe(object instance, Type type, out IDescription description)
        {
            if (instance == null) { description = new Null(); return true; }
            else if (References.TryGetValue(instance, out description)) return true;
            else if (World.Container.TryGet<IDescriptor>(type, out var descriptor))
                return descriptor.Describe(instance, out description, With(type));
            else { description = default; return false; }
        }

        public bool Describe<T>(in T instance, out IDescription description)
        {
            if (instance == null) { description = new Null(); return true; }
            else if (References.TryGetValue(instance, out description)) return true;
            else if (World.Container.TryGet<T, Descriptor<T>>(out var descriptor))
                return descriptor.Describe(instance, out description, With<T>());
            else return Describe(instance, typeof(T), out description);
        }

        public DescribeContext With<T>() => With(TypeUtility.GetData<T>());
        public DescribeContext With(Type type = null) => With(TypeUtility.GetData(type));
        public DescribeContext With(TypeData type = null) => new DescribeContext(type ?? Type, References, World);
    }

    public readonly struct InstantiateContext
    {
        public readonly TypeData Type;
        public readonly Dictionary<IDescription, object> References;
        public readonly World World;

        public InstantiateContext(Dictionary<IDescription, object> references, World world) : this(null, references, world) { }
        public InstantiateContext(TypeData type, Dictionary<IDescription, object> references, World world)
        {
            Type = type;
            References = references;
            World = world;
        }

        public bool Instantiate<T>(IDescription description, out T instance)
        {
            if (description is Null) { instance = default; return true; }
            else if (References.TryGetValue(description, out var value)) { instance = (T)value; return true; }
            else if (World.Container.TryGet<T, Descriptor<T>>(out var descriptor))
                return descriptor.Instantiate(description, out instance, With<T>());
            else if (Instantiate(description, typeof(T), out value)) { instance = (T)value; return true; }
            else { instance = default; return false; }
        }

        public bool Instantiate(IDescription description, Type type, out object instance)
        {
            if (description is Null) { instance = default; return true; }
            else if (References.TryGetValue(description, out instance)) return true;
            else if (World.Container.TryGet<IDescriptor>(type, out var instantiator))
                return instantiator.Instantiate(description, out instance, With(type));
            else return false;
        }


        public InstantiateContext With<T>() => With(TypeUtility.GetData<T>());
        public InstantiateContext With(Type type = null) => With(TypeUtility.GetData(type));
        public InstantiateContext With(TypeData type = null) => new InstantiateContext(type ?? Type, References, World);
    }

    public static class Extensions
    {
        public static bool Describe(this World world, object instance, Type type, out IDescription description) =>
            new DescribeContext(new Dictionary<object, IDescription>(), world).Describe(instance, type, out description);
        public static bool Describe<T>(this World world, in T instance, out IDescription description) =>
            new DescribeContext(new Dictionary<object, IDescription>(), world).Describe(instance, out description);

        public static bool Instantiate<T>(this World world, IDescription description, out T instance) =>
            new InstantiateContext(new Dictionary<IDescription, object>(), world).Instantiate(description, out instance);
        public static bool Instantiate(this World world, IDescription description, Type type, out object instance) =>
            new InstantiateContext(new Dictionary<IDescription, object>(), world).Instantiate(description, type, out instance);
    }

    public static class Descriptor
    {
        public interface IMember<T>
        {
            string Identifier { get; }
            bool Describe(in T instance, out IDescription description, in DescribeContext context);
            bool Instantiate(IDescription description, ref T instance, in InstantiateContext context);
        }

        public sealed class Field<T, TValue> : IMember<T>
        {
            public delegate ref readonly TValue Getter(in T instance);

            public string Identifier { get; }
            public readonly Getter Get;

            public Field(string identifier, Getter get) { Identifier = identifier; Get = get; }

            public bool Describe(in T instance, out IDescription description, in DescribeContext context) =>
                context.Describe(Get(instance), out description);

            public bool Instantiate(IDescription description, ref T instance, in InstantiateContext context)
            {
                if (context.Instantiate(description, out TValue value))
                {
                    UnsafeUtility.Set(Get(instance), value);
                    return true;
                }
                return false;
            }
        }

        public sealed class Property<T, TValue> : IMember<T>
        {
            public delegate TValue Getter(in T instance);
            public delegate void Setter(ref T instance, in TValue value);

            public string Identifier { get; }
            public readonly Getter Get;
            public readonly Setter Set;

            public Property(string identifier, Getter get, Setter set) { Identifier = identifier; Get = get; Set = set; }

            public bool Describe(in T instance, out IDescription description, in DescribeContext context) =>
                context.Describe(Get(instance), out description);

            public bool Instantiate(IDescription description, ref T instance, in InstantiateContext context)
            {
                if (context.Instantiate(description, out TValue value))
                {
                    Set(ref instance, value);
                    return true;
                }
                return false;
            }
        }

        sealed class ConcreteObject<T> : Descriptor<T>
        {
            public readonly Func<T> Construct;
            public readonly IMember<T>[] Members;

            readonly Dictionary<string, IMember<T>> _members;

            public ConcreteObject(Func<T> construct, params IMember<T>[] members)
            {
                Construct = construct;
                Members = members;
                _members = members.ToDictionary(member => member.Identifier);
            }

            public override bool Describe(in T instance, out IDescription description, in DescribeContext context)
            {
                var members = new Dictionary<string, IDescription>(Members.Length);
                description = new ConcreteObject(members);
                context.References[instance] = description;
                for (int i = 0; i < Members.Length; i++)
                {
                    var member = Members[i];
                    if (member.Describe(instance, out var value, context)) members[member.Identifier] = value;
                    else return false;
                }
                return true;
            }

            public override bool Instantiate(IDescription description, out T instance, in InstantiateContext context)
            {
                switch (description)
                {
                    case ConcreteObject @object:
                        instance = Construct();
                        context.References[description] = instance;
                        foreach (var pair in @object.Members)
                        {
                            if (_members.TryGetValue(pair.Key, out var member))
                                member.Instantiate(pair.Value, ref instance, context);
                        }
                        return true;
                    default: instance = default; return false;
                }
            }
        }

        sealed class Mapper<TIn, TOut> : Descriptor<TIn>
        {
            public readonly InFunc<TIn, TOut> To;
            public readonly InFunc<TOut, TIn> From;
            public Mapper(InFunc<TIn, TOut> to, InFunc<TOut, TIn> from) { To = to; From = from; }

            public override bool Describe(in TIn instance, out IDescription description, in DescribeContext context)
            {
                if (context.Describe(To(instance), out description))
                {
                    context.References[instance] = description;
                    return true;
                }
                return false;
            }

            public override bool Instantiate(IDescription description, out TIn instance, in InstantiateContext context)
            {
                if (context.Instantiate(description, out TOut value))
                {
                    instance = From(value);
                    context.References[description] = instance;
                    return true;
                }
                instance = default;
                return false;
            }
        }

        public static IMember<T> Member<T, TValue>(string identifier, Field<T, TValue>.Getter get) =>
            new Field<T, TValue>(identifier, get);

        public static IMember<T> Member<T, TValue>(Field<T, TValue>.Getter get) =>
            Member("", get);

        public static IMember<T> Member<T, TValue>(string identifier, Property<T, TValue>.Getter get, Property<T, TValue>.Setter set) =>
            new Property<T, TValue>(identifier, get, set);

        public static IMember<T> Member<T, TValue>(Property<T, TValue>.Getter get, Property<T, TValue>.Setter set) =>
            Member("", get, set);

        public static Descriptor<T> Object<T>(Func<T> construct, params IMember<T>[] members) =>
            new ConcreteObject<T>(construct, members);

        public static Descriptor<TIn> Map<TIn, TOut>(InFunc<TIn, TOut> to, InFunc<TOut, TIn> from) =>
            new Mapper<TIn, TOut>(to, from);
    }

    public sealed class ListDescriptor<T> : Descriptor<List<T>>
    {
        public override bool Describe(in List<T> instance, out IDescription description, in DescribeContext context)
        {
            var values = new T[instance.Count];
            var items = new IDescription[1];
            instance.CopyTo(values, 0);
            description = new ConcreteArray(items);
            context.References[instance] = description;
            return context.Describe(values, out items[0]);
        }

        public override bool Instantiate(IDescription description, out List<T> instance, in InstantiateContext context)
        {
            instance = new List<T>();
            context.References[description] = instance;
            if (description is ConcreteArray array && array.Items.Length == 1 &&
                context.Instantiate(array.Items[0], out T[] values))
            {
                instance.AddRange(values);
                return true;
            }
            return false;
        }
    }

    public sealed class ListDescriptor : Descriptor<IList>
    {
        public override bool Describe(in IList instance, out IDescription description, in DescribeContext context)
        {
            var values = Array.CreateInstance(context.Type.Arguments[0], instance.Count);
            var items = new IDescription[1];
            instance.CopyTo(values, 0);
            description = new ConcreteArray(items);
            context.References[instance] = description;
            return context.Describe(values, out items[0]);
        }

        public override bool Instantiate(IDescription description, out IList instance, in InstantiateContext context)
        {
            instance = (IList)Activator.CreateInstance(context.Type);
            context.References[description] = instance;
            if (description is ConcreteArray array && array.Items.Length == 1 &&
                context.Instantiate(array.Items[0], out Array values))
            {
                for (int i = 0; i < values.Length; i++) instance.Add(values.GetValue(i));
                return true;
            }
            instance = default;
            return false;
        }
    }

    public sealed class DictionaryDescriptor<TKey, TValue> : Descriptor<Dictionary<TKey, TValue>>
    {
        public override bool Describe(in Dictionary<TKey, TValue> instance, out IDescription description, in DescribeContext context)
        {
            var keys = new TKey[instance.Count];
            var values = new TValue[instance.Count];
            var items = new IDescription[2];
            instance.Keys.CopyTo(keys, 0);
            instance.Values.CopyTo(values, 0);
            description = new ConcreteArray(items);
            context.References[instance] = description;
            return context.Describe(keys, out items[0]) && context.Describe(values, out items[1]);
        }

        public override bool Instantiate(IDescription description, out Dictionary<TKey, TValue> instance, in InstantiateContext context)
        {
            instance = new Dictionary<TKey, TValue>();
            context.References[description] = instance;
            if (description is ConcreteArray array && array.Items.Length == 2 &&
                context.Instantiate(array.Items[0], out TKey[] keys) &&
                context.Instantiate(array.Items[1], out TValue[] values) &&
                keys.Length == values.Length)
            {
                for (int i = 0; i < keys.Length; i++) instance.Add(keys[i], values[i]);
                return true;
            }
            return false;
        }
    }

    public sealed class DictionaryDescriptor : Descriptor<IDictionary>
    {
        public override bool Describe(in IDictionary instance, out IDescription description, in DescribeContext context)
        {
            var keys = Array.CreateInstance(context.Type.Arguments[0], instance.Count);
            var values = Array.CreateInstance(context.Type.Arguments[1], instance.Count);
            var items = new IDescription[2];
            instance.Keys.CopyTo(keys, 0);
            instance.Values.CopyTo(values, 0);
            description = new ConcreteArray(items);
            context.References[instance] = description;
            return context.Describe(keys, out items[0]) && context.Describe(values, out items[1]);
        }

        public override bool Instantiate(IDescription description, out IDictionary instance, in InstantiateContext context)
        {
            instance = (IDictionary)Activator.CreateInstance(context.Type);
            context.References[description] = instance;
            if (description is ConcreteArray array && array.Items.Length == 2 &&
                context.Instantiate(array.Items[0], out Array keys) &&
                context.Instantiate(array.Items[1], out Array values) &&
                keys.Length == values.Length)
            {
                for (int i = 0; i < keys.Length; i++) instance.Add(keys.GetValue(i), values.GetValue(i));
                return true;
            }
            return false;
        }
    }

    public sealed class ObjectDescriptor : IDescriptor
    {
        public bool Describe(object instance, out IDescription description, in DescribeContext context)
        {
            var concrete = instance.GetType();
            if (concrete != context.Type)
            {
                if (context.Describe(concrete, out var type) && context.Describe(instance, concrete, out var value))
                {
                    description = new Abstract(type, value);
                    context.References[instance] = description;
                    return true;
                }
                description = default;
                return false;
            }

            if (context.Type.IsShallow)
            {
                description = new Constant(instance);
                context.References[instance] = description;
                return true;
            }

            switch (instance)
            {
                default:
                    var fields = context.Type.InstanceFields;
                    var members = new Dictionary<string, IDescription>(fields.Length);
                    description = new ConcreteObject(members);
                    context.References[instance] = description;
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        if (context.Describe(field.GetValue(instance), field.FieldType, out var value))
                            members[field.Name] = value;
                        else return false;
                    }
                    return true;
            }
        }

        public bool Instantiate(IDescription description, out object instance, in InstantiateContext context)
        {
            switch (description)
            {
                case Constant constant:
                    instance = context.Type.IsPlain ? constant.Value : CloneUtility.Shallow(constant.Value);
                    return true;
                case Abstract @object:
                    {
                        if (context.Instantiate(@object.Type, out Type type) &&
                            context.Instantiate(@object.Value, type, out instance))
                            return true;
                        instance = default;
                        return false;
                    }
                case ConcreteObject @object:
                    {
                        instance = FormatterServices.GetUninitializedObject(context.Type);
                        context.References[description] = instance;
                        var type = TypeUtility.GetData(context.Type);
                        foreach (var pair in @object.Members)
                        {
                            if (type.Fields.TryGetValue(pair.Key, out var field) &&
                                context.Instantiate(pair.Value, field.FieldType, out var value))
                                field.SetValue(instance, value);
                        }
                        return true;
                    }
                default: instance = default; return false;
            }
        }
    }

    public sealed class ArrayDescriptor : Descriptor<Array>
    {
        public override bool Describe(in Array instance, out IDescription description, in DescribeContext context)
        {
            var data = TypeUtility.GetData(instance.GetType());
            if (data.IsShallow)
            {
                description = new Constant(instance);
                context.References[instance] = description;
                return true;
            }

            var items = new IDescription[instance.Length];
            description = new ConcreteArray(items);
            context.References[instance] = description;
            for (int i = 0; i < items.Length; i++)
            {
                if (context.Describe(instance.GetValue(i), data.Element, out var item)) items[i] = item;
                else return false;
            }
            return true;
        }

        public override bool Instantiate(IDescription description, out Array instance, in InstantiateContext context)
        {
            switch (description)
            {
                case Constant @object:
                    instance = (Array)CloneUtility.Shallow(@object.Value);
                    context.References[description] = instance;
                    return true;
                case ConcreteArray array:
                    {
                        var type = TypeUtility.GetData(context.Type);
                        var element = type.Element;
                        instance = Array.CreateInstance(element, array.Items.Length);
                        context.References[description] = instance;
                        for (int i = 0; i < array.Items.Length; i++)
                        {
                            var item = array.Items[i];
                            if (context.Instantiate(item, element, out var value)) instance.SetValue(value, i);
                        }
                        return true;
                    }
                default: instance = default; return false;
            }
        }
    }

    public sealed class TypeDescriptor : Descriptor<Type>
    {
        public override bool Describe(in Type instance, out IDescription description, in DescribeContext context)
        {
            var items = new IDescription[3];
            description = new ConcreteArray(items);
            context.References[instance] = description;
            return
                context.Describe(instance.Module, out items[0]) &&
                context.Describe(instance.MetadataToken, out items[1]) &&
                context.Describe(instance.IsGenericType ? instance.GetGenericArguments() : Type.EmptyTypes, out items[2]);
        }

        public override bool Instantiate(IDescription description, out Type instance, in InstantiateContext context)
        {
            if (description is ConcreteArray array && array.Items.Length == 3 &&
                context.Instantiate(array.Items[0], out Module module) &&
                context.Instantiate(array.Items[1], out int token) &&
                context.Instantiate(array.Items[2], out Type[] arguments))
            {
                instance = module.ResolveType(token);
                if (arguments.Length > 0) instance = instance.MakeGenericType(arguments);
                context.References[description] = instance;
                return true;
            }
            instance = default;
            return false;
        }
    }

    public sealed class AssemblyDescriptor : Descriptor<Assembly>
    {
        public override bool Describe(in Assembly instance, out IDescription description, in DescribeContext context)
        {
            var items = new IDescription[1];
            description = new ConcreteArray(items);
            context.References[instance] = description;
            return context.Describe(instance.GetName().Name, out items[0]);
        }

        public override bool Instantiate(IDescription description, out Assembly instance, in InstantiateContext context)
        {
            if (description is ConcreteArray array && array.Items.Length == 1 &&
                context.Instantiate(array.Items[0], out string name))
            {
                instance = Assembly.Load(name);
                context.References[description] = instance;
                return true;
            }
            instance = default;
            return false;
        }
    }

    public sealed class ModuleDescriptor : Descriptor<Module>
    {
        public override bool Describe(in Module instance, out IDescription description, in DescribeContext context)
        {
            var items = new IDescription[1];
            description = new ConcreteArray(items);
            context.References[instance] = description;
            return context.Describe(instance.Assembly, out items[0]);
        }

        public override bool Instantiate(IDescription description, out Module instance, in InstantiateContext context)
        {
            if (description is ConcreteArray array && array.Items.Length == 1 &&
                context.Instantiate(array.Items[0], out Assembly assembly))
            {
                instance = assembly.ManifestModule;
                context.References[description] = instance;
                return true;
            }
            instance = default;
            return false;
        }
    }

    public sealed class MethodDescriptor : Descriptor<MethodInfo>
    {
        public override bool Describe(in MethodInfo instance, out IDescription description, in DescribeContext context)
        {
            var items = new IDescription[3];
            description = new ConcreteArray(items);
            context.References[instance] = description;
            return
                context.Describe(instance.DeclaringType, out items[0]) &&
                context.Describe(instance.MetadataToken, out items[1]) &&
                context.Describe(instance.IsGenericMethod ? instance.GetGenericArguments() : Type.EmptyTypes, out items[2]);
        }

        public override bool Instantiate(IDescription description, out MethodInfo instance, in InstantiateContext context)
        {
            if (description is ConcreteArray array && array.Items.Length == 3 &&
                context.Instantiate(array.Items[0], out Type type) &&
                context.Instantiate(array.Items[1], out int token) &&
                context.Instantiate(array.Items[2], out Type[] arguments))
            {
                var data = TypeUtility.GetData(type);
                if (data.Members.TryGetValue(token, out var member) && member is MethodInfo casted)
                {
                    instance = casted;
                    if (arguments.Length > 0) instance = instance.MakeGenericMethod(arguments);
                    context.References[description] = instance;
                    return true;
                }
            }
            instance = default;
            return false;
        }
    }

    // public sealed class DelegateDescriptor : Descriptor<Delegate>
    // {
    //     public override bool Describe(in Delegate instance, out IDescription description, in DescribeContext context)
    //     {
    //         var invocations = instance.GetInvocationList();
    //         if (invocations.Length == 1)
    //         {
    //             var items = new IDescription[3];
    //             description = new ConcreteArray(items);
    //             context.References[instance] = description;
    //             return
    //                 context.Describe(instance.Method, out items[0]) &&
    //                 context.Describe(instance.Target, out items[1]);
    //         }
    //         return context.Describe(invocations, out description);
    //     }

    //     public override bool Instantiate(IDescription description, out Delegate instance, in InstantiateContext context)
    //     {
    //         if (description is ConcreteArray array && array.Items.Length == 3 &&
    //             context.Instantiate(array.Items[0], out Type type) &&
    //             context.Instantiate(array.Items[1], out int token) &&
    //             context.Instantiate(array.Items[2], out Type[] arguments))
    //         {
    //             var data = TypeUtility.GetData(type);
    //             if (data.Members.TryGetValue(token, out var member) && member is Delegate casted)
    //             {
    //                 instance = casted;
    //                 if (arguments.Length > 0) instance = instance.MakeGenericMethod(arguments);
    //                 context.References[description] = instance;
    //                 return true;
    //             }
    //         }
    //         instance = default;
    //         return false;
    //     }
    // }

    public sealed class MemberDescriptor : Descriptor<MemberInfo>
    {
        public override bool Describe(in MemberInfo instance, out IDescription description, in DescribeContext context)
        {
            var items = new IDescription[2];
            description = new ConcreteArray(items);
            context.References[instance] = description;
            return
                context.Describe(instance.Module, out items[0]) &&
                context.Describe(instance.MetadataToken, out items[1]);
        }

        public override bool Instantiate(IDescription description, out MemberInfo instance, in InstantiateContext context)
        {
            if (description is ConcreteArray array && array.Items.Length == 3 &&
                context.Instantiate(array.Items[0], out Module module) &&
                context.Instantiate(array.Items[1], out int token))
            {
                instance = module.ResolveMember(token);
                context.References[description] = instance;
                return true;
            }
            instance = default;
            return false;
        }
    }

    public sealed class ConstantDescriptor : IDescriptor
    {
        public bool Describe(object instance, out IDescription description, in DescribeContext context)
        {
            description = new Constant(instance);
            context.References[instance] = description;
            return true;
        }

        public bool Instantiate(IDescription description, out object instance, in InstantiateContext context)
        {
            if (description is Constant constant)
            {
                instance = constant.Value;
                context.References[description] = instance;
                return true;
            }
            instance = default;
            return false;
        }
    }

    public sealed class ConstantDescriptor<T> : Descriptor<T>
    {
        public override bool Describe(in T instance, out IDescription description, in DescribeContext context)
        {
            description = new Constant(instance);
            context.References[instance] = description;
            return true;
        }

        public override bool Instantiate(IDescription description, out T instance, in InstantiateContext context)
        {
            if (description is Constant constant)
            {
                instance = (T)constant.Value;
                context.References[description] = instance;
                return true;
            }
            instance = default;
            return false;
        }
    }


    public readonly struct SerializeContext
    {
        public readonly Writer Writer;
        public readonly Dictionary<IDescription, int> References;
        public readonly World World;

        public bool Serialize(IDescription description) => throw null;
    }

    public readonly struct DeserializeContext
    {
        public readonly Reader Reader;
        public readonly List<IDescription> References;
        public readonly World World;

        public bool Deserialize(out IDescription description) => throw null;
    }

    namespace Serializers
    {
        public interface ISerializer : ITrait
        {
            bool Serialize(IDescription description, in SerializeContext context);
            bool Deserialize(out IDescription description, in DeserializeContext context);
        }

        public abstract class Serializer<T> : ISerializer where T : IDescription
        {
            public abstract bool Serialize(in T description, in SerializeContext context);
            public abstract bool Deserialize(out T description, in DeserializeContext context);

            bool ISerializer.Serialize(IDescription description, in SerializeContext context) =>
                description is T casted && Serialize(casted, context);

            bool ISerializer.Deserialize(out IDescription description, in DeserializeContext context)
            {
                if (Deserialize(out var casted, context))
                {
                    description = casted;
                    return true;
                }
                description = default;
                return false;
            }
        }


        public sealed class Default : ISerializer
        {
            public enum Kinds : byte { Null, Constant, Abstract, Array, Object, Reference }
            public enum Types : byte
            {
                Null,
                Boolean, Char,
                Byte, SByte, UShort, Short, UInt, Int, ULong, Long,
                Float, Double, Decimal,
                String, DateTime, TimeSpan,
                Array, Object
            }

            static Types ToType(Type type)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Empty: return Types.Null;
                    case TypeCode.Boolean: return Types.Boolean;
                    case TypeCode.Byte: return Types.Byte;
                    case TypeCode.Char: return Types.Char;
                    case TypeCode.DateTime: return Types.DateTime;
                    case TypeCode.Decimal: return Types.Decimal;
                    case TypeCode.Double: return Types.Double;
                    case TypeCode.Int16: return Types.Short;
                    case TypeCode.Int32: return Types.Int;
                    case TypeCode.Int64: return Types.Long;
                    case TypeCode.SByte: return Types.SByte;
                    case TypeCode.Single: return Types.Float;
                    case TypeCode.String: return Types.String;
                    case TypeCode.UInt16: return Types.UShort;
                    case TypeCode.UInt32: return Types.UInt;
                    case TypeCode.UInt64: return Types.ULong;
                }

                return
                    type == typeof(TimeSpan) ? Types.TimeSpan :
                    type.IsArray ? Types.Array :
                    Types.Object;
            }

            static bool WriteValue(object value, Type type, Types code, Writer writer)
            {
                switch (code)
                {
                    case Types.Null: return true;
                    case Types.Boolean: writer.Write((bool)value); return true;
                    case Types.Char: writer.Write((char)value); return true;
                    case Types.Byte: writer.Write((byte)value); return true;
                    case Types.SByte: writer.Write((sbyte)value); return true;
                    case Types.UShort: writer.Write((ushort)value); return true;
                    case Types.Short: writer.Write((short)value); return true;
                    case Types.UInt: writer.Write((uint)value); return true;
                    case Types.Int: writer.Write((int)value); return true;
                    case Types.ULong: writer.Write((ulong)value); return true;
                    case Types.Long: writer.Write((long)value); return true;
                    case Types.Float: writer.Write((float)value); return true;
                    case Types.Double: writer.Write((double)value); return true;
                    case Types.Decimal: writer.Write((decimal)value); return true;
                    case Types.String: writer.Write((string)value); return true;
                    case Types.DateTime: writer.Write((DateTime)value); return true;
                    case Types.TimeSpan: writer.Write((TimeSpan)value); return true;
                    case Types.Array:
                        {
                            var array = (Array)value;
                            var data = TypeUtility.GetData(type);
                            var element = ToType(data.Element);
                            writer.Write(element);
                            writer.Write(array.Length);
                            for (int i = 0; i < array.Length; i++)
                            {
                                if (WriteValue(array.GetValue(i), data.Element, element, writer)) continue;
                                return false;
                            }
                            return true;
                        }
                    // case Types.Object:
                    //     {
                    //         var data = TypeUtility.GetData(type);

                    //     }
                    default: return false;
                }
            }

            public bool Serialize(IDescription description, in SerializeContext context)
            {
                if (context.References.TryGetValue(description, out var index))
                {
                    context.Writer.Write(Kinds.Reference);
                    context.Writer.Write(index);
                    return true;
                }

                context.References[description] = context.References.Count;
                switch (description)
                {
                    case Null _: context.Writer.Write(Kinds.Null); return true;
                    case Constant constant:
                        if (constant.Value is null) { context.Writer.Write(Kinds.Null); return true; }
                        context.Writer.Write(Kinds.Constant);
                        var type = constant.Value.GetType();
                        var code = ToType(type);
                        context.Writer.Write(code);
                        return WriteValue(constant.Value, type, code, context.Writer);
                    case Abstract @abstract:
                        context.Writer.Write(Kinds.Abstract);
                        return context.Serialize(@abstract.Type) && context.Serialize(@abstract.Value);
                    case ConcreteObject @object:
                        context.Writer.Write(Kinds.Object);
                        context.Writer.Write(@object.Members.Count);
                        foreach (var pair in @object.Members)
                        {
                            context.Writer.Write(pair.Key);
                            if (context.Serialize(pair.Value)) continue;
                            return false;
                        }
                        return true;
                    case ConcreteArray array:
                        context.Writer.Write(Kinds.Array);
                        context.Writer.Write(array.Items.Length);
                        for (int i = 0; i < array.Items.Length; i++)
                        {
                            if (context.Serialize(array.Items[i])) continue;
                            return false;
                        }
                        return true;
                    default: return false;
                }
            }

            public bool Deserialize(out IDescription description, in DeserializeContext context)
            {
                context.Reader.Read(out Kinds kind);
                switch (kind)
                {
                    case Kinds.Null: description = new Null(); return true;
                    case Kinds.Reference:
                        context.Reader.Read(out int index);
                        description = context.References[index];
                        return true;
                    case Kinds.Constant:
                        {
                            context.Reader.Read(out Types type);
                            switch (type)
                            {
                                case Types.Null: description = new Constant(null); return true;
                                case Types.Boolean: { context.Reader.Read(out bool value); description = new Constant(value); return true; }
                                case Types.Char: { context.Reader.Read(out char value); description = new Constant(value); return true; }
                                case Types.Byte: { context.Reader.Read(out byte value); description = new Constant(value); return true; }
                                case Types.SByte: { context.Reader.Read(out sbyte value); description = new Constant(value); return true; }
                                case Types.UShort: { context.Reader.Read(out ushort value); description = new Constant(value); return true; }
                                case Types.Short: { context.Reader.Read(out short value); description = new Constant(value); return true; }
                                case Types.UInt: { context.Reader.Read(out uint value); description = new Constant(value); return true; }
                                case Types.Int: { context.Reader.Read(out int value); description = new Constant(value); return true; }
                                case Types.ULong: { context.Reader.Read(out ulong value); description = new Constant(value); return true; }
                                case Types.Long: { context.Reader.Read(out long value); description = new Constant(value); return true; }
                                case Types.Float: { context.Reader.Read(out float value); description = new Constant(value); return true; }
                                case Types.Double: { context.Reader.Read(out double value); description = new Constant(value); return true; }
                                case Types.Decimal: { context.Reader.Read(out decimal value); description = new Constant(value); return true; }
                                case Types.String: { context.Reader.Read(out string value); description = new Constant(value); return true; }
                                case Types.DateTime: { context.Reader.Read(out DateTime value); description = new Constant(value); return true; }
                                case Types.TimeSpan: { context.Reader.Read(out TimeSpan value); description = new Constant(value); return true; }
                            }
                            break;
                        }
                    case Kinds.Abstract:
                        {
                            if (context.Deserialize(out var type) && context.Deserialize(out var value))
                            {
                                description = new Abstract(type, value);
                                return true;
                            }
                            break;
                        }
                    case Kinds.Object:
                        {
                            context.Reader.Read(out int count);
                            var members = new Dictionary<string, IDescription>(count);
                            description = new ConcreteObject(members);
                            for (int i = 0; i < count; i++)
                            {
                                if (context.Reader.Read(out string key) && context.Deserialize(out var value))
                                    members.Add(key, value);
                                else return false;
                            }
                            return true;
                        }
                    case Kinds.Array:
                        {
                            context.Reader.Read(out int count);
                            var items = new IDescription[count];
                            description = new ConcreteArray(items);
                            for (int i = 0; i < items.Length; i++)
                            {
                                if (context.Deserialize(out items[i])) continue;
                                return false;
                            }
                            return true;
                        }
                }
                description = default;
                return false;
            }
        }
    }

    public interface IDescription { }

    public sealed class Null : IDescription { }

    public sealed class Constant : IDescription
    {
        public readonly object Value;
        public Constant(object value) { Value = value; }
    }

    public sealed class Abstract : IDescription
    {
        public readonly IDescription Type;
        public readonly IDescription Value;
        public Abstract(IDescription type, IDescription value) { Type = type; Value = value; }
    }

    public sealed class ConcreteObject : IDescription
    {
        public readonly Dictionary<string, IDescription> Members;
        public ConcreteObject(Dictionary<string, IDescription> members) => Members = members;
    }

    public sealed class ConcreteArray : IDescription
    {
        public readonly IDescription[] Items;
        public ConcreteArray(params IDescription[] items) { Items = items; }
    }
}