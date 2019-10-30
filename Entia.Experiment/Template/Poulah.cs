using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core;
using Entia.Experimental.Instantiators;
using Entia.Experimental.Values;
using Entia.Modules;

namespace Entia.Experimental
{
    namespace Values
    {
        public interface IValue { }

        /*
            - includes primitive types, strings, types, assemblies and some other types
            - includes structs that have no fields
            - includes structs that implement no interface or only empty interfaces or interfaces with only getters
                - while it's true that a boxed struct may mutate its fields through the 'System.Object' methods,
                this would clearly be looking for trouble, and so it is assumed that this is not the case
                - same goes with getters
            - warning: if the struct is being modified by reflection, there may be a problem...
                - could be simpler and safer to just clone the struct
        */
        public readonly struct Primitive : IValue, IImplementation<Primitive.Instantiator>
        {
            sealed class Instantiator : Instantiator<Primitive>
            {
                public override bool Instantiate(in Primitive value, in Context context, out object instance)
                {
                    instance = value.Value;
                    return true;
                }
            }

            public readonly object Value;
            public Primitive(object value) { Value = value; }
        }

        public readonly struct Object : IValue, IImplementation<Object.Instantiator>
        {
            sealed class Instantiator : Instantiator<Object>
            {
                public override bool Instantiate(in Object value, in Context context, out object instance)
                {
                    instance = CloneUtility.Shallow(value.Value);
                    foreach (var (field, reference) in value.Fields)
                        field.SetValue(instance, context.Instances[reference]);
                    foreach (var (property, reference) in value.Properties)
                        property.SetValue(instance, context.Instances[reference]);
                    return true;
                }
            }

            // this is a clone of the original value
            public readonly object Value;
            public readonly (FieldInfo field, int reference)[] Fields;
            public readonly (PropertyInfo property, int reference)[] Properties;

            public Object(object value, (FieldInfo field, int reference)[] fields, (PropertyInfo property, int reference)[] properties)
            {
                Value = value;
                Fields = fields;
                Properties = properties;
            }
        }

        public readonly struct Array : IValue, IImplementation<Array.Instantiator>
        {
            sealed class Instantiator : Instantiator<Array>
            {
                public override bool Instantiate(in Array value, in Context context, out object instance)
                {
                    var array = CloneUtility.Shallow(value.Value);
                    instance = array;
                    foreach (var (index, reference) in value.Items)
                        array.SetValue(context.Instances[reference], index);
                    return true;
                }
            }

            // this is a clone of the original value
            public readonly System.Array Value;
            public readonly (int index, int reference)[] Items;

            public Array(System.Array value, params (int index, int reference)[] items)
            {
                Value = value;
                Items = items;
            }
        }

        public readonly struct Entity : IValue, IImplementation<Entity.Instantiator>
        {
            sealed class Instantiator : Instantiator<Entity>
            {
                public override bool Instantiate(in Entity value, in Context context, out object instance)
                {
                    var entity = context.World.Entities().Create();
                    instance = entity;
                    for (int i = 0; i < value.Components.Length; i++)
                    {
                        if (context.Instances[value.Components[i]] is IComponent component)
                            context.World.Components().Set(entity, component);
                        else
                            return false;
                    }
                    return true;
                }
            }

            public readonly int[] Children;
            public readonly int[] Components;

            public Entity(int[] children, int[] components)
            {
                Children = children;
                Components = components;
            }
        }
    }

    namespace Instantiators
    {
        public readonly struct Context
        {
            public readonly IValue Value;
            public readonly object[] Instances;
            public readonly World World;

            public Context(IValue value, object[] instances, World world)
            {
                Value = value;
                Instances = instances;
                World = world;
            }
        }

        public interface IInstantiator : ITrait
        {
            bool Instantiate(in Context context, out object instance);
        }

        public abstract class Instantiator<T> : IInstantiator where T : IValue
        {
            public abstract bool Instantiate(in T value, in Context context, out object instance);
            bool IInstantiator.Instantiate(in Context context, out object instance)
            {
                if (context.Value is T value) return Instantiate(value, context, out instance);
                instance = default;
                return false;
            }
        }
    }

    namespace Templaters
    {
        public readonly struct Context
        {
            public readonly object Value;
            public readonly List<IValue> Values;
            public readonly Dictionary<object, int> References;
            public readonly World World;

            public Context(World world) : this(null, new List<IValue> { null }, new Dictionary<object, int>(), world) { }
            public Context(object value, List<IValue> values, Dictionary<object, int> references, World world)
            {
                Value = value;
                Values = values;
                References = references;
                World = world;
            }

            public int Template(object value)
            {
                if (value is null) return 0;
                else if (References.TryGetValue(value, out var index)) return index;
                else if (World.Container.TryGet<ITemplater>(value.GetType(), out var templater))
                {
                    index = Reserve(value);
                    Values[index] = templater.Template(With(value));
                    return index;
                }
                else return 0;
            }

            public int Reserve(object value)
            {
                var index = Values.Count;
                References[value] = index;
                Values.Add(default);
                return index;
            }

            public Context With(object value) => new Context(value, Values, References, World);
        }

        public interface ITemplater : ITrait,
            IImplementation<Entity, Null>,
            IImplementation<System.Array, Array>,
            IImplementation<object, Object>
        {
            IValue Template(in Context context);
        }

        public abstract class Templater<T> : ITemplater
        {
            public abstract IValue Template(in T value, in Context context);
            IValue ITemplater.Template(in Context context) =>
                context.Value is T value ? Template(value, context) : null;
        }

        public sealed class Object : ITemplater
        {
            public IValue Template(in Context context)
            {
                var value = context.Value;
                if (Templating.IsPrimitive(value)) return new Values.Primitive(value);

                var fields = value.GetType().InstanceFields();
                var members = new List<(FieldInfo, int)>(fields.Length);
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    var member = field.GetValue(value);
                    if (Templating.IsPrimitive(member)) continue;
                    members.Add((field, context.Template(member)));
                }
                return new Values.Object(CloneUtility.Shallow(value), members.ToArray(), System.Array.Empty<(PropertyInfo, int)>());
            }
        }

        public sealed class Array : Templater<System.Array>
        {
            public override IValue Template(in System.Array value, in Context context)
            {
                var items = new List<(int, int)>(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    var item = value.GetValue(i);
                    if (Templating.IsPrimitive(item)) continue;
                    items.Add((i, context.Template(value)));
                }
                return new Values.Array(CloneUtility.Shallow(value), items.ToArray());
            }
        }

        public sealed class Null : ITemplater
        {
            public IValue Template(in Context context) => null;
        }
    }

    public static class Templating
    {
        public static bool IsPrimitive(object value)
        {
            if (value is null) return true;
            var type = value.GetType();
            return type.IsPrimitive || value is string || value is ICustomAttributeProvider;
        }

        public static Template Template(this World world, object value)
        {
            var context = new Templaters.Context(world);
            var index = context.Template(value);
            return new Template(index, context.Values.ToArray());
        }

        public static Template<Entity> Template(this World world, Entity entity)
        {
            var context = new Templaters.Context(world);
            var (index, resolve) = context.Descend(entity);
            resolve();
            return new Template<Entity>(index, context.Values.ToArray());
        }

        public static object Instantiate(this World world, in Template template)
        {
            var instances = new object[template.Values.Length];
            for (int i = template.Values.Length - 1; i >= 0; i--)
            {
                var value = template.Values[i];
                if (value is null)
                    instances[i] = null;
                else if (world.Container.TryGet<IInstantiator>(value.GetType(), out var instantiator))
                    instantiator.Instantiate(new Context(value, instances, world), out instances[i]);
                else
                    instances[i] = value;
            }
            return instances[template.Index];
        }

        public static T Instantiate<T>(this World world, in Template<T> template) =>
            world.Instantiate(template) is T value ? value : default;

        static (int index, Action resolve) Descend(this Templaters.Context context, Entity entity)
        {
            var index = context.Reserve(entity);
            var children = context.World.Families().Children(entity);
            var components = context.World.Components().Get(entity).ToArray();
            var value = new Values.Entity(new int[children.Count], new int[components.Length]);
            var resolve = new Action(() =>
            {
                for (int i = 0; i < components.Length; i++)
                    value.Components[i] = context.Template(components[i]);
            });
            context.Values[index] = value;
            for (int i = 0; i < children.Count; i++)
            {
                var pair = context.Descend(children[i]);
                value.Children[i] = pair.index;
                resolve += pair.resolve;
            }
            return (index, resolve);
        }
    }

    public readonly struct Template<T>
    {
        public static implicit operator Template(in Template<T> template) =>
            new Template(template.Index, template.Values);

        public readonly int Index;
        public readonly IValue[] Values;

        public Template(int index, params IValue[] values)
        {
            Index = index;
            Values = values;
        }
    }

    public readonly struct Template
    {
        public readonly int Index;
        public readonly IValue[] Values;

        public Template(int index, params IValue[] values)
        {
            Index = index;
            Values = values;
        }
    }
}