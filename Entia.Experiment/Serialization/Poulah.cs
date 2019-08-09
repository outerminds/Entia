using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Serialization;

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

    public struct DescribeContext
    {
        public TypeData Type;
        public Descriptors Descriptors;
        public Dictionary<object, IDescription> References;

        public bool Describe(object instance, Type type, out IDescription description) =>
            Descriptors.Describe(instance, out description, this.With(type));
        public bool Describe<T>(in T instance, out IDescription description) =>
            Descriptors.Describe(instance, out description, this.With<T>());

        public DescribeContext With<T>() => With(TypeUtility.GetData<T>());
        public DescribeContext With(Type type = null) => With(TypeUtility.GetData(type));
        public DescribeContext With(TypeData type = null) => new DescribeContext
        {
            Type = type ?? Type,
            Descriptors = Descriptors,
            References = References
        };
    }

    public struct InstantiateContext
    {
        public TypeData Type;
        public Descriptors Descriptors;
        public Dictionary<IDescription, object> References;

        public bool Instantiate<T>(IDescription description, out T instance) =>
            Descriptors.Instantiate(description, out instance, With<T>());
        public bool Instantiate(IDescription description, out object instance, Type type) =>
            Descriptors.Instantiate(description, out instance, With(type));

        public InstantiateContext With<T>() => With(TypeUtility.GetData<T>());
        public InstantiateContext With(Type type = null) => With(TypeUtility.GetData(type));
        public InstantiateContext With(TypeData type = null) => new InstantiateContext
        {
            Type = type ?? Type,
            Descriptors = Descriptors,
            References = References
        };
    }

    public sealed class Descriptors : IModule
    {
        readonly Container _container;
        public Descriptors(Container container) { _container = container; }

        public bool Describe(object instance, Type type, out IDescription description) =>
            Describe(instance, out description, DescribeContext(TypeUtility.GetData(type)));
        public bool Describe<T>(in T instance, out IDescription description) =>
            Describe(instance, out description, DescribeContext(TypeUtility.GetData<T>()));

        public bool Describe(object instance, out IDescription description, in DescribeContext context)
        {
            if (instance == null)
            {
                description = new Null();
                return true;
            }
            else if (context.References.TryGetValue(instance, out description)) return true;
            else if (_container.TryGet<IDescriptor>(context.Type, out var descriptor))
                return descriptor.Describe(instance, out description, context);

            description = default;
            return false;
        }

        public bool Describe<T>(in T instance, out IDescription description, in DescribeContext context)
        {
            if (instance == null)
            {
                description = new Null();
                return true;
            }
            else if (context.References.TryGetValue(instance, out description)) return true;
            else if (_container.TryGet<T, Descriptor<T>>(out var descriptor))
                return descriptor.Describe(instance, out description, context);
            else
                return Describe((object)instance, out description, context);
        }

        public bool Instantiate<T>(IDescription description, out T instance) =>
            Instantiate(description, out instance, InstantiateContext(TypeUtility.GetData<T>()));
        public bool Instantiate(IDescription description, out object instance, Type type) =>
            Instantiate(description, out instance, InstantiateContext(TypeUtility.GetData(type)));

        public bool Instantiate<T>(IDescription description, out T instance, in InstantiateContext context)
        {
            if (description is Null)
            {
                instance = default;
                return true;
            }
            else if (context.References.TryGetValue(description, out var value))
            {
                instance = (T)value;
                return true;
            }
            else if (_container.TryGet<T, Descriptor<T>>(out var descriptor))
                return descriptor.Instantiate(description, out instance, context);
            else if (Instantiate(description, out value, context))
            {
                instance = (T)value;
                return true;
            }

            instance = default;
            return false;
        }

        public bool Instantiate(IDescription description, out object instance, in InstantiateContext context)
        {
            if (description is Null)
            {
                instance = default;
                return true;
            }
            else if (context.References.TryGetValue(description, out instance)) return true;
            else if (_container.TryGet<IDescriptor>(context.Type, out var instantiator))
                return instantiator.Instantiate(description, out instance, context);
            return false;
        }

        DescribeContext DescribeContext(TypeData type) => new DescribeContext
        {
            Type = type,
            Descriptors = this,
            References = new Dictionary<object, IDescription>()
        };

        InstantiateContext InstantiateContext(TypeData type) => new InstantiateContext
        {
            Type = type,
            Descriptors = this,
            References = new Dictionary<IDescription, object>()
        };
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
            instance = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(context.Type.Arguments));
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
            instance = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(context.Type.Arguments));
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
                            context.Instantiate(@object.Description, out instance, type))
                            return true;
                        instance = default;
                        return false;
                    }
                case ConcreteArray array:
                    {
                        instance = FormatterServices.GetUninitializedObject(context.Type);
                        context.References[description] = instance;
                        var type = TypeUtility.GetData(context.Type);
                        var fields = type.InstanceFields;
                        var count = Math.Min(array.Items.Length, fields.Length);
                        for (int i = 0; i < array.Items.Length; i++)
                        {
                            var item = array.Items[i];
                            var field = fields[i];
                            if (context.Instantiate(item, out var value, field.FieldType))
                                field.SetValue(instance, value);
                        }
                        return true;
                    }
                case ConcreteObject @object:
                    {
                        instance = FormatterServices.GetUninitializedObject(context.Type);
                        context.References[description] = instance;
                        var type = TypeUtility.GetData(context.Type);
                        foreach (var pair in @object.Members)
                        {
                            if (type.Fields.TryGetValue(pair.Key, out var field) &&
                                context.Instantiate(pair.Value, out var value, field.FieldType))
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
                            if (context.Instantiate(item, out var value, element)) instance.SetValue(value, i);
                        }
                        return true;
                    }
                case ConcreteObject @object:
                    {
                        var type = TypeUtility.GetData(context.Type);
                        var element = type.Element;
                        var index = 0;
                        instance = Array.CreateInstance(element, @object.Members.Count);
                        foreach (var pair in @object.Members)
                        {
                            if (context.Instantiate(pair.Value, out var value, element)) instance.SetValue(value, index);
                            index++;
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

    public interface IDescription { }

    public interface IByteSerializer : ITrait
    {
        bool Serialize(IDescription description, Writer writer, World world);
        bool Deserialize(out IDescription description, Reader reader, World world);
    }

    public interface IStringSerializer : ITrait
    {
        bool Serialize(IDescription description, Writer writer, World world);
        bool Deserialize(out IDescription description, Reader reader, World world);
    }

    public sealed class Null : IDescription { }

    public sealed class Constant : IDescription
    {
        public readonly object Value;
        public Constant(object value) { Value = value; }
    }

    public sealed class Abstract : IDescription
    {
        public readonly IDescription Type;
        public readonly IDescription Description;
        public Abstract(IDescription type, IDescription description) { Type = type; Description = description; }
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

    // public static class Description
    // {
    //     enum Kinds : byte { Null, Byte, Single, Double, String, Type, Assembly, Object, Array, Reference }

    //     public static byte[] Serialize(IDescription description)
    //     {
    //         using (var writer = new Writer())
    //         {
    //             Serialize(description, writer, new Dictionary<IDescription, ushort>());
    //             return writer.ToArray();
    //         }
    //     }

    //     static void Serialize(IDescription description, Writer writer, Dictionary<IDescription, ushort> references)
    //     {
    //         references[description] = (ushort)references.Count;
    //         switch (description)
    //         {
    //             case Null _: writer.Write(Kinds.Null); break;
    //             case ConstantObject<string> value:
    //                 writer.Write(Kinds.String);
    //                 writer.Write(value.Value);
    //                 break;
    //             case ConstantObject<byte> value:
    //                 writer.Write(Kinds.Byte);
    //                 writer.Write(value.Value);
    //                 break;
    //             case AbstractType value:
    //                 writer.Write(Kinds.Type);
    //                 Serialize(value.Assembly, writer, references);
    //                 writer.Write(value.Value.FullName);
    //                 break;
    //             case AbstractAssembly value:
    //                 writer.Write(Kinds.Assembly);
    //                 writer.Write(value.Value.FullName);
    //                 break;
    //             case IConstantObject value:
    //                 Serialize(Describe(value.Value), writer, references);
    //                 break;
    //             case ConcreteArray value:
    //                 writer.Write(Kinds.Array);
    //                 writer.Write(value.Items.Length);
    //                 for (int i = 0; i < value.Items.Length; i++) Serialize(value.Items[i], writer, references);
    //                 break;
    //             case IConstantArray value:
    //                 Serialize(Describe(value.Values), writer, references);
    //                 break;
    //             case ConcreteObject value:
    //                 writer.Write(Kinds.Object);
    //                 writer.Write(value.Members.Length);
    //                 for (int i = 0; i < value.Members.Length; i++)
    //                 {
    //                     var member = value.Members[i];
    //                     Serialize(member.Identifier, writer, references);
    //                     Serialize(member.Value, writer, references);
    //                 }
    //                 break;
    //             case Reference value:
    //                 writer.Write(Kinds.Reference);
    //                 writer.Write(references[value.Description]);
    //                 break;
    //         }
    //     }

    //     public static IDescription Deserialize(byte[] bytes)
    //     {
    //         using (var reader = new Reader(bytes))
    //         {
    //             return Deserialize(reader, new List<IDescription>());
    //         }
    //     }

    //     static IDescription Deserialize(Reader reader, List<IDescription> references)
    //     {
    //         var index = references.Count;
    //         references.Add(default);
    //         reader.Read(out Kinds kind);
    //         switch (kind)
    //         {
    //             case Kinds.Null: return new Null();
    //             case Kinds.String: { reader.Read(out string value); return references[index] = new ConstantObject<string>(value); }
    //             case Kinds.Byte: { reader.Read(out byte value); return references[index] = new ConstantObject<byte>(value); }
    //             case Kinds.Array:
    //                 {
    //                     reader.Read(out int count);
    //                     var items = new IDescription[count];
    //                     var description = new ConcreteArray(items);
    //                     references[index] = description;
    //                     for (int i = 0; i < count; i++) items[i] = Deserialize(reader, references);
    //                     return description;
    //                 }
    //             case Kinds.Object:
    //                 {
    //                     reader.Read(out int count);
    //                     var members = new Member[count];
    //                     var description = new ConcreteObject(members);
    //                     references[index] = description;
    //                     for (int i = 0; i < count; i++)
    //                         members[i] = new Member(Deserialize(reader, references), Deserialize(reader, references));
    //                     return description;
    //                 }
    //             case Kinds.Assembly:
    //                 {
    //                     reader.Read(out string name);
    //                     var assembly = System.Reflection.Assembly.Load(name);
    //                     return references[index] = new AbstractAssembly(assembly);
    //                 }
    //             case Kinds.Type:
    //                 {
    //                     var assembly = Deserialize(reader, references) as AbstractAssembly;
    //                     reader.Read(out string name);
    //                     var type = assembly.Value.GetType(name);
    //                     return references[index] = new AbstractType(type, assembly);
    //                 }
    //             case Kinds.Reference:
    //                 reader.Read(out ushort reference);
    //                 return new Reference(references[reference]);
    //             default: return new Null();
    //         }
    //     }
    // }
}