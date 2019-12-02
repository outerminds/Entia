using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Family;
using System.Reflection;
using Entia.Experimental.Boba.Templates;
using System.Runtime.Serialization;
using Entia.Experimental.Instantiators;

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
        public interface ITemplate : IComponent { }

        public struct Primitive : ITemplate
        {
            [Implementation]
            sealed class Instantiator : Instantiators.Instantiator<Primitive>
            {
                public override bool Instantiate(in Node<Primitive> node, in Instantiators.Context context, out object instance)
                {
                    instance = CloneUtility.Shallow(node.Component.Value);
                    return true;
                }
            }

            public object Value;
        }

        public struct Array : ITemplate
        {
            public Type Element;
        }

        public struct Object : ITemplate
        {
            public Type Type;
        }
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