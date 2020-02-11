using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Family;
using System.Runtime.Serialization;
using System.Reflection;

namespace Entia.Experimental.Boba
{
    [DebuggerTypeProxy(typeof(Node<>.View))]
    public readonly struct Node<T> where T : IComponent
    {
        sealed class View
        {
            public Entity Entity => _node.Entity;
            public T Component => _node.Component;
            public Option<Node<IComponent>> Root => _node.Root<IComponent>();
            public Option<Node<IComponent>> Parent => _node.Parent<IComponent>();
            public Node<IComponent>[] Children => _node.Children<IComponent>().ToArray();
            public Node<IComponent>[] Ancestors => _node.Ancestors<IComponent>().ToArray();
            public Node<IComponent>[] Descendants => _node.Descendants<IComponent>(From.Top).ToArray();

            readonly Node<T> _node;

            public View(Node<T> node) { _node = node; }
        }

        public static implicit operator Entity(in Node<T> node) => node.Entity;

        public readonly Entity Entity;
        public readonly T Component;

        readonly Entia.Modules.Families _families;
        readonly Entia.Modules.Components _components;

        public Node(Entity entity, in T component, Entia.Modules.Families families, Entia.Modules.Components components)
        {
            Entity = entity;
            Component = component;
            _families = families;
            _components = components;
        }

        public bool Is<TComponent>() where TComponent : IComponent => Component is TComponent;

        public Node<TComponent> As<TComponent>() where TComponent : IComponent
        {
            if (Component is TComponent component) return new Node<TComponent>(Entity, component, _families, _components);
            return default;
        }

        public Entity Parent() => _families.Parent(Entity);
        public Slice<Entity>.Read Children() => _families.Children(Entity);
        public IEnumerable<Entity> Ancestors() => _families.Ancestors(Entity);
        public IEnumerable<Entity> Descendants(From from) => _families.Descendants(Entity, from);

        public Option<Node<TComponent>> TryAs<TComponent>() where TComponent : IComponent
        {
            if (Component is TComponent component) return new Node<TComponent>(Entity, component, _families, _components);
            return Option.None();
        }
        public bool TryAs<TComponent>(out Node<TComponent> node) where TComponent : IComponent =>
            TryAs<TComponent>().TryValue(out node);

        public Option<TComponent> Get<TComponent>() where TComponent : IComponent
        {
            if (_components.TryGet<TComponent>(Entity, out var component)) return component;
            return Option.None();
        }
        public bool TryGet<TComponent>(out TComponent component) where TComponent : IComponent =>
            Get<TComponent>().TryValue(out component);

        public Option<Node<TComponent>> Root<TComponent>() where TComponent : IComponent =>
            Parent<TComponent>().TryValue(out var parent) ? parent.Root<TComponent>() : TryAs<TComponent>();
        public Option<Node<TComponent>> Parent<TComponent>() where TComponent : IComponent => Of<TComponent>(Parent());
        public Option<Node<TComponent>> Child<TComponent>() where TComponent : IComponent =>
            Children<TComponent>().FirstOrNone();
        public IEnumerable<Node<TComponent>> Children<TComponent>() where TComponent : IComponent =>
            Children().Select(Of<TComponent>).Choose();
        public Option<Node<TComponent>> Ancestor<TComponent>() where TComponent : IComponent =>
            Ancestors<TComponent>().FirstOrNone();
        public IEnumerable<Node<TComponent>> Ancestors<TComponent>() where TComponent : IComponent =>
            Ancestors().Select(Of<TComponent>).Choose();
        public IEnumerable<Node<TComponent>> Descendants<TComponent>(From from) where TComponent : IComponent =>
            Descendants(from).Select(Of<TComponent>).Choose();
        public Option<Node<TComponent>> Descendant<TComponent>(From from) where TComponent : IComponent =>
            Descendants<TComponent>(from).FirstOrNone();

        Option<Node<TComponent>> Of<TComponent>(Entity entity) where TComponent : IComponent
        {
            if (_components.TryGet<TComponent>(entity, out var component))
                return new Node<TComponent>(entity, component, _families, _components);
            return Option.None();
        }

        public override string ToString() => $"{Entity}: {Component}";
    }

    namespace Instantiators
    {
        public readonly struct Context
        {
            public readonly Node<Templates.ITemplate> Node;
            public readonly Modules.Components Components;
            public readonly Families Families;
            public readonly World World;

            public Context(World world) : this(default, world.Components(), world.Families(), world) { }
            public Context(Node<Templates.ITemplate> node, Modules.Components components, Families families, World world)
            {
                Node = node;
                Components = components;
                Families = families;
                World = world;
            }

            public bool Instantiate(Entity template, out object instance)
            {
                if (Components.TryGet<Templates.ITemplate>(template, out var component) &&
                    World.Container.TryGet<IInstantiator>(component.GetType(), out var instantiator))
                    return instantiator.Instantiate(With(new Node<Templates.ITemplate>(template, component, Families, Components)), out instance);

                instance = default;
                return false;
            }

            public Context With(Node<Templates.ITemplate>? node = null) => new Context(node ?? Node, Components, Families, World);
        }

        public interface IInstantiator : ITrait
        {
            bool Instantiate(in Context context, out object instance);
        }

        public abstract class Instantiator<T> : IInstantiator where T : Templates.ITemplate
        {
            public abstract bool Instantiate(in Node<T> node, in Instantiators.Context context, out object instance);

            bool IInstantiator.Instantiate(in Instantiators.Context context, out object instance)
            {
                if (context.Node.TryAs<T>(out var node)) return Instantiate(node, context, out instance);
                instance = default;
                return false;
            }
        }

        public delegate bool Instantiate<T>(in Node<T> node, in Context context, out object instance) where T : Templates.ITemplate;
        public static class Instantiator
        {
            public static Instantiator<T> From<T>(Instantiate<T> instantiate) where T : Templates.ITemplate => new Function<T>(instantiate);
        }

        public sealed class Function<T> : Instantiator<T> where T : Templates.ITemplate
        {
            readonly Instantiate<T> _instantiate;
            public Function(Instantiate<T> instantiate) { _instantiate = instantiate; }
            public override bool Instantiate(in Node<T> node, in Context context, out object instance) => _instantiate(node, context, out instance);
        }
    }

    namespace Templates
    {
        // - only terminal nodes should hold values?
        public interface ITemplate : IComponent { }

        public interface IConverter : ITrait
        {
            bool Convert(in ConvertToContext context, out Node node);
            bool Instantiate(in ConvertFromContext context, out object instance);
            bool Initialize(in ConvertFromContext context, ref object instance);
        }

        public abstract class Converter<T> : IConverter
        {
            public abstract bool Convert(in T instance, in ConvertToContext context, out Node node);
            public abstract bool Instantiate(in ConvertFromContext context, out T instance);
            public virtual bool Initialize(in ConvertFromContext context, ref T instance) => true;

            bool IConverter.Convert(in ConvertToContext context, out Node node)
            {
                if (context.Instance is T instance) return Convert(instance, context, out node);
                node = default;
                return false;
            }

            bool IConverter.Instantiate(in ConvertFromContext context, out object instance)
            {
                if (Instantiate(context, out var casted))
                {
                    instance = casted;
                    return true;
                }
                instance = default;
                return false;
            }

            bool IConverter.Initialize(in ConvertFromContext context, ref object instance)
            {
                if (instance is T casted && Initialize(context, ref casted))
                {
                    instance = casted;
                    return true;
                }
                return false;
            }
        }

        public readonly struct Node
        {
            static readonly ITemplate _object = new Object();
            static readonly ITemplate _array = new Array();
            static readonly ITemplate _member = new Member();

            public static readonly Node Null = new Node(new Primitive { Type = TypeCode.Empty });
            public static readonly Node True = new Node(new Primitive { Type = TypeCode.Boolean, Value = true });
            public static readonly Node False = new Node(new Primitive { Type = TypeCode.Boolean, Value = false });
            public static Node Array(params Node[] items) => new Node(_array, items);
            public static Node Object(params Node[] members) => new Node(_object, members);
            public static Node Member(Node key, Node value) => new Node(_member, key, value);

            public readonly ITemplate Value;
            public readonly Node[] Children;

            public Node(ITemplate value, params Node[] children)
            {
                Value = value;
                Children = children;
            }
        }

        public static class NodeExtensions
        {
            public static bool TryMember(in this Node node, out Node key, out Node value)
            {
                if (node.Children.Length == 2 && node.Value is Member)
                {
                    key = node.Children[0];
                    value = node.Children[1];
                    return true;
                }

                key = default;
                value = default;
                return false;
            }

            public static bool TryAbstract(in this Node node, out Node type, out Node value)
            {
                if (node.Children.Length == 2 && node.Value is Abstract)
                {
                    type = node.Children[0];
                    value = node.Children[1];
                    return true;
                }

                type = default;
                value = default;
                return false;
            }
        }

        public readonly struct ConvertToContext
        {
            public readonly object Instance;
            public readonly TypeData Type;
            public readonly Entity Entity;
            public readonly Modules.Entities Entities;
            public readonly Modules.Families Families;
            public readonly Modules.Components Components;
            public readonly World World;

            public Node<T> Create<T>(in T template, params Entity[] children) where T : ITemplate
            {
                var entity = Entities.Create();
                Families.Adopt(entity, children);
                return new Node<T>(entity, template, Families, Components);
            }

            public bool Convert2<T>(in T instance, out Entity entity) => throw null;
            public bool Convert2(object instance, Type type, out Entity entity) => throw null;
            public bool Convert2(object instance, TypeData type, out Entity entity) => throw null;
            public bool Convert<T>(in T instance, out Node node) => throw null;
            public bool Convert(object instance, Type type, out Node node) => throw null;
            public bool Convert(object instance, TypeData type, out Node node) => throw null;
        }

        public readonly struct ConvertFromContext
        {
            public readonly Node Node;
            public readonly TypeData Type;
            public readonly Node<ITemplate> Node2;
            public readonly World World;

            public bool Convert2<T>(Entity entity, out T instance) => throw null;
            public bool Convert2(Entity entity, Type type, out object instance) => throw null;
            public bool Convert2(Entity entity, TypeData type, out object instance) => throw null;
            public bool Convert<T>(in Node node, out T instance) => throw null;
            public bool Convert(in Node node, Type type, out object instance) => throw null;
            public bool Convert(in Node node, TypeData type, out object instance) => throw null;
        }

        public struct Blittable : ITemplate
        {
            public int Size;
            public object Value;
        }

        public struct Primitive : ITemplate
        {
            public static implicit operator Primitive(bool value) => new Primitive { Type = TypeCode.Boolean, Value = value };
            public static implicit operator Primitive(char value) => new Primitive { Type = TypeCode.Char, Value = value };
            public static implicit operator Primitive(byte value) => new Primitive { Type = TypeCode.Byte, Value = value };
            public static implicit operator Primitive(sbyte value) => new Primitive { Type = TypeCode.SByte, Value = value };
            public static implicit operator Primitive(short value) => new Primitive { Type = TypeCode.Int16, Value = value };
            public static implicit operator Primitive(ushort value) => new Primitive { Type = TypeCode.UInt16, Value = value };
            public static implicit operator Primitive(int value) => new Primitive { Type = TypeCode.Int32, Value = value };
            public static implicit operator Primitive(uint value) => new Primitive { Type = TypeCode.UInt32, Value = value };
            public static implicit operator Primitive(long value) => new Primitive { Type = TypeCode.Int64, Value = value };
            public static implicit operator Primitive(ulong value) => new Primitive { Type = TypeCode.UInt64, Value = value };
            public static implicit operator Primitive(float value) => new Primitive { Type = TypeCode.Single, Value = value };
            public static implicit operator Primitive(double value) => new Primitive { Type = TypeCode.Double, Value = value };
            public static implicit operator Primitive(decimal value) => new Primitive { Type = TypeCode.Decimal, Value = value };
            public static implicit operator Primitive(string value) => new Primitive { Type = TypeCode.String, Value = value };

            public TypeCode Type;
            public object Value;
        }

        public unsafe sealed class BlittableObject<T> : Converter<T> where T : unmanaged
        {
            public override bool Convert(in T instance, in ConvertToContext context, out Node node)
            {
                node = new Node(new Blittable { Size = sizeof(T), Value = instance });
                return true;
            }

            public override bool Instantiate(in ConvertFromContext context, out T instance)
            {
                if (context.Node.Value is Blittable template && template.Value is T value)
                {
                    instance = value;
                    return true;
                }
                instance = default;
                return false;
            }
        }

        public unsafe sealed class BlittableObject : IConverter
        {
            public bool Convert(in ConvertToContext context, out Node node)
            {
                if (context.Type.Size is int size)
                {
                    node = new Node(new Blittable
                    {
                        Size = size,
                        Value = CloneUtility.Shallow(context.Instance)
                    });
                    return true;
                }
                node = default;
                return false;
            }

            public bool Instantiate(in ConvertFromContext context, out object instance)
            {
                if (context.Node.Value is Blittable template)
                {
                    instance = CloneUtility.Shallow(template.Value);
                    return true;
                }
                instance = default;
                return false;
            }

            public bool Initialize(in ConvertFromContext context, ref object instance) => true;
        }

        public unsafe sealed class BlittableArray<T> : Converter<T[]> where T : unmanaged
        {
            public override bool Convert(in T[] instance, in ConvertToContext context, out Node node)
            {
                node = new Node(new Blittable
                {
                    Size = sizeof(T) * instance.Length,
                    Value = CloneUtility.Shallow(instance)
                });
                return true;
            }

            public override bool Instantiate(in ConvertFromContext context, out T[] instance)
            {
                if (context.Node.Value is Blittable template && template.Value is T[] value)
                {
                    instance = CloneUtility.Shallow(value);
                    return true;
                }
                instance = default;
                return false;
            }
        }

        public sealed class BlittableArray : Converter<System.Array>
        {
            public override bool Convert(in System.Array instance, in ConvertToContext context, out Node node)
            {
                if (context.Type.Element.Size is int size)
                {
                    node = new Node(new Blittable
                    {
                        Size = size * instance.Length,
                        Value = CloneUtility.Shallow(instance)
                    });
                    return true;
                }
                node = default;
                return false;
            }

            public override bool Instantiate(in ConvertFromContext context, out System.Array instance)
            {
                if (context.Node.Value is Blittable template && template.Value is System.Array value)
                {
                    instance = CloneUtility.Shallow(value);
                    return true;
                }
                instance = default;
                return false;
            }
        }

        public sealed class ConcretePrimitive<T> : Converter<T>
        {
            public override bool Convert(in T instance, in ConvertToContext context, out Node node)
            {
                node = new Node(new Primitive { Type = context.Type.Code, Value = instance });
                return true;
            }

            public override bool Instantiate(in ConvertFromContext context, out T instance)
            {
                if (context.Node.Value is Primitive template && template.Value is T value)
                {
                    instance = value;
                    return true;
                }
                instance = default;
                return false;
            }
        }

        public sealed class ConcreteArray<T> : Converter<T[]>
        {
            public override bool Convert(in T[] instance, in ConvertToContext context, out Node node)
            {
                var items = new Node[instance.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    if (context.Convert(instance[i], out items[i])) continue;
                    node = default;
                    return false;
                }
                node = Node.Array(items);
                return true;
            }

            public override bool Instantiate(in ConvertFromContext context, out T[] instance)
            {
                instance = new T[context.Node.Children.Length];
                return true;
            }

            public override bool Initialize(in ConvertFromContext context, ref T[] instance)
            {
                var items = context.Node.Children;
                for (int i = 0; i < items.Length; i++)
                {
                    if (context.Convert(items[i], out instance[i])) continue;
                    return false;
                }
                return true;
            }
        }

        public sealed class ConcreteArray : Converter<System.Array>
        {
            public bool Convert2(in System.Array instance, in ConvertToContext context, out Node<Array> node)
            {
                var element = context.Type.Element;
                var items = new Entity[instance.Length];
                for (int i = 0; i < instance.Length; i++)
                {
                    if (context.Convert2(instance.GetValue(i), element, out items[i])) continue;
                    node = default;
                    return false;
                }
                node = context.Create(new Array(), items);
                return true;
            }

            public bool Instantiate2(in Node<Array> node, in ConvertFromContext context, out System.Array instance)
            {
                instance = System.Array.CreateInstance(context.Type.Element, node.Children().Count);
                return true;
            }

            public bool Initialize2(in Node<Array> node, in ConvertFromContext context, ref System.Array instance)
            {
                var element = context.Type.Element;
                var items = node.Children();
                for (int i = 0; i < items.Count; i++)
                {
                    if (context.Convert2(items[i], element, out var item))
                        instance.SetValue(item, i);
                    else
                        return false;
                }
                return true;
            }

            public override bool Convert(in System.Array instance, in ConvertToContext context, out Node node)
            {
                var element = context.Type.Element;
                var items = new Node[instance.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    if (context.Convert(instance.GetValue(i), element, out items[i])) continue;
                    node = default;
                    return false;
                }
                node = Node.Array(items);
                return true;
            }

            public override bool Instantiate(in ConvertFromContext context, out System.Array instance)
            {
                instance = System.Array.CreateInstance(context.Type.Element, context.Node.Children.Length);
                return true;
            }

            public override bool Initialize(in ConvertFromContext context, ref System.Array instance)
            {
                var element = context.Type.Element;
                var items = context.Node.Children;
                for (int i = 0; i < items.Length; i++)
                {
                    if (context.Convert(items[i], element, out var item))
                    {
                        instance.SetValue(item, i);
                        continue;
                    }
                    return false;
                }
                return true;
            }
        }

        public sealed class ConcreteObject : IConverter
        {
            public bool Convert(in ConvertToContext context, out Node node)
            {
                var fields = context.Type.InstanceFields;
                var members = new Node[fields.Length];
                for (int i = 0; i < members.Length; i++)
                {
                    var field = fields[i];
                    if (context.Convert(field.Name, out var key) &&
                        context.Convert(field.GetValue(context.Instance), field.FieldType, out var value))
                    {
                        members[i] = Node.Member(key, value);
                        continue;
                    }
                    node = default;
                    return false;
                }
                node = Node.Object(members);
                return true;
            }

            public bool Instantiate(in ConvertFromContext context, out object instance)
            {
                instance = FormatterServices.GetUninitializedObject(context.Type);
                return true;
            }

            public bool Initialize(in ConvertFromContext context, ref object instance)
            {
                var members = context.Node.Children;
                for (int i = 0; i < members.Length; i++)
                {
                    if (members[i].TryMember(out var keyNode, out var valueNode) &&
                        context.Convert<string>(keyNode, out var key) &&
                        context.Type.InstanceMembers.TryGetValue(key, out var member) &&
                        member is FieldInfo field &&
                        context.Convert(valueNode, field.FieldType, out var value))
                        field.SetValue(instance, value);
                }
                return true;
            }
        }

        // - children represent the ordered items
        public struct Array : ITemplate { }
        // - children represent the members of the object
        public struct Object : ITemplate { }
        // - has 2 children, 1 for the key, 1 for the value
        public struct Member : ITemplate { }
        // - has 2 children, 1 for the type, 1 for the value
        // - removes the need for other nodes to hold their type
        // - type representation can have many forms (string, array of subtypes)
        public struct Abstract : ITemplate { }
        // - terminal node that holds a reference to another node
        public struct Reference : ITemplate { public Entity Value; }
    }

    namespace Templating
    {
        public static class Extensions
        {
            public static void Add<T>(this Container container, Instantiators.Instantiator<T> instantiator) where T : Templates.ITemplate =>
                container.Add<T, Instantiators.IInstantiator>(instantiator);
        }

        public static class Template
        {
            public static Result<object> Instantiate(this World world, Entity template)
            {
                var context = new Instantiators.Context(world);
                context.Instantiate(template, out var instance);
                return instance;
            }
        }
    }
}