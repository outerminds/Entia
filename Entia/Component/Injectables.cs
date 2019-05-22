using Entia.Components;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Injectables
{
    /// <summary>
    /// Gives access to all component operations.
    /// </summary>
    public readonly struct AllComponents : IInjectable, IEnumerable<IComponent>
    {
        [ThreadSafe]
        public readonly struct Write : IInjectable, IEnumerable<IComponent>
        {
            [Injector]
            static Injector<Write> Injector => Injectors.Injector.From(world => new Write(world.Components()));
            [Depender]
            static IDepender Depender => Dependers.Depender.From(
                new Dependencies.Read(typeof(Entity)),
                new Dependencies.Write(typeof(IComponent)));

            readonly Modules.Components _components;

            /// <summary>
            /// Initializes a new instance of the <see cref="AllComponents"/> struct.
            /// </summary>
            /// <param name="components"></param>
            public Write(Modules.Components components) { _components = components; }

            /// <inheritdoc cref="Modules.Components.Default{T}()"/>
            public T Default<T>() where T : struct, IComponent => _components.Default<T>();
            /// <inheritdoc cref="Modules.Components.TryDefault(Type, out IComponent)"/>
            public bool TryDefault(Type type, out IComponent component) => _components.TryDefault(type, out component);
            /// <inheritdoc cref="Modules.Components.Get{T}(Entity, States)"/>
            public ref T Get<T>(Entity entity, States include = States.All) where T : struct, IComponent => ref _components.Get<T>(entity, include);
            /// <inheritdoc cref="Modules.Components.GetOrDummy{T}(Entity, out bool, States)"/>
            public ref T GetOrDummy<T>(Entity entity, out bool success, States include = States.All) where T : struct, IComponent => ref _components.GetOrDummy<T>(entity, out success, include);
            /// <inheritdoc cref="Modules.Components.TryGet{T}(Entity, out T, States)"/>
            public bool TryGet<T>(Entity entity, out T component, States include = States.All) where T : struct, IComponent => _components.TryGet(entity, out component, include);
            /// <inheritdoc cref="Modules.Components.TryGet(Entity, Type, out IComponent, States)"/>
            public bool TryGet(Entity entity, Type type, out IComponent component, States include = States.All) => _components.TryGet(entity, type, out component, include);
            /// <inheritdoc cref="Modules.Components.Get(Entity, States)"/>
            public IEnumerable<IComponent> Get(Entity entity, States include = States.All) => _components.Get(entity, include);
            /// <inheritdoc cref="Modules.Components.Get{T}(States)"/>
            public IEnumerable<(Entity entity, T component)> Get<T>(States include = States.All) where T : struct, IComponent => _components.Get<T>(include);
            /// <inheritdoc cref="Modules.Components.Get(Type, States)"/>
            public IEnumerable<(Entity entity, IComponent component)> Get(Type type, States include = States.All) => _components.Get(type, include);
            /// <inheritdoc cref="Modules.Components.Has{T}(Entity, States)"/>
            public bool Has<T>(Entity entity, States include = States.All) where T : IComponent => _components.Has<T>(entity, include);
            /// <inheritdoc cref="Modules.Components.Has(Entity, Type, States)"/>
            public bool Has(Entity entity, Type type, States include = States.All) => _components.Has(entity, type, include);
            /// <inheritdoc cref="Modules.Components.Count(Entity, States)"/>
            public int Count(Entity entity, States include = States.All) => _components.Count(entity, include);
            /// <inheritdoc cref="Modules.Components.Count{T}(States)"/>
            public int Count<T>(States include = States.All) where T : IComponent => _components.Count<T>(include);
            /// <inheritdoc cref="Modules.Components.Count(Type, States)"/>
            public int Count(Type type, States include = States.All) => _components.Count(type, include);
            /// <inheritdoc cref="Modules.Components.Count(States)"/>
            public int Count(States include = States.All) => _components.Count(include);
            /// <inheritdoc cref="Modules.Components.State{T}(Entity)"/>
            public States State<T>(Entity entity) where T : struct, IComponent => _components.State<T>(entity);
            /// <inheritdoc cref="Modules.Components.State(Entity, Type)"/>
            public States State(Entity entity, Type type) => _components.State(entity, type);
            /// <inheritdoc cref="Modules.Components.GetEnumerator()"/>
            public IEnumerator<IComponent> GetEnumerator() => _components.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _components.GetEnumerator();
        }

        [ThreadSafe]
        public readonly struct Read : IInjectable, IEnumerable<IComponent>
        {
            [Injector]
            static Injector<Read> Injector => Injectors.Injector.From(world => new Read(world.Components()));
            [Depender]
            static IDepender Depender => Dependers.Depender.From(
                new Dependencies.Read(typeof(Entity)),
                new Dependencies.Read(typeof(IComponent)));

            readonly Modules.Components _components;

            /// <summary>
            /// Initializes a new instance of the <see cref="AllComponents"/> struct.
            /// </summary>
            /// <param name="components"></param>
            public Read(Modules.Components components) { _components = components; }

            /// <inheritdoc cref="Modules.Components.Default{T}()"/>
            public T Default<T>() where T : struct, IComponent => _components.Default<T>();
            /// <inheritdoc cref="Modules.Components.TryDefault(Type, out IComponent)"/>
            public bool TryDefault(Type type, out IComponent component) => _components.TryDefault(type, out component);
            /// <inheritdoc cref="Modules.Components.Get{T}(Entity, States)"/>
            public ref readonly T Get<T>(Entity entity, States include = States.All) where T : struct, IComponent => ref _components.Get<T>(entity, include);
            /// <inheritdoc cref="Modules.Components.GetOrDummy{T}(Entity, out bool, States)"/>
            public ref readonly T GetOrDummy<T>(Entity entity, out bool success, States include = States.All) where T : struct, IComponent => ref _components.GetOrDummy<T>(entity, out success, include);
            /// <inheritdoc cref="Modules.Components.TryGet{T}(Entity, out T, States)"/>
            public bool TryGet<T>(Entity entity, out T component, States include = States.All) where T : struct, IComponent => _components.TryGet(entity, out component, include);
            /// <inheritdoc cref="Modules.Components.TryGet(Entity, Type, out IComponent, States)"/>
            public bool TryGet(Entity entity, Type type, out IComponent component, States include = States.All) => _components.TryGet(entity, type, out component, include);
            /// <inheritdoc cref="Modules.Components.Get(Entity, States)"/>
            public IEnumerable<IComponent> Get(Entity entity, States include = States.All) => _components.Get(entity, include);
            /// <inheritdoc cref="Modules.Components.Get{T}(States)"/>
            public IEnumerable<(Entity entity, T component)> Get<T>(States include = States.All) where T : struct, IComponent => _components.Get<T>(include);
            /// <inheritdoc cref="Modules.Components.Get(Type, States)"/>
            public IEnumerable<(Entity entity, IComponent component)> Get(Type type, States include = States.All) => _components.Get(type, include);
            /// <inheritdoc cref="Modules.Components.Has{T}(Entity, States)"/>
            public bool Has<T>(Entity entity, States include = States.All) where T : IComponent => _components.Has<T>(entity, include);
            /// <inheritdoc cref="Modules.Components.Has(Entity, Type, States)"/>
            public bool Has(Entity entity, Type type, States include = States.All) => _components.Has(entity, type, include);
            /// <inheritdoc cref="Modules.Components.Count(Entity, States)"/>
            public int Count(Entity entity, States include = States.All) => _components.Count(entity, include);
            /// <inheritdoc cref="Modules.Components.Count{T}(States)"/>
            public int Count<T>(States include = States.All) where T : IComponent => _components.Count<T>(include);
            /// <inheritdoc cref="Modules.Components.Count(Type, States)"/>
            public int Count(Type type, States include = States.All) => _components.Count(type, include);
            /// <inheritdoc cref="Modules.Components.Count(States)"/>
            public int Count(States include = States.All) => _components.Count(include);
            /// <inheritdoc cref="Modules.Components.State{T}(Entity)"/>
            public States State<T>(Entity entity) where T : struct, IComponent => _components.State<T>(entity);
            /// <inheritdoc cref="Modules.Components.State(Entity, Type)"/>
            public States State(Entity entity, Type type) => _components.State(entity, type);
            /// <inheritdoc cref="Modules.Components.GetEnumerator()"/>
            public IEnumerator<IComponent> GetEnumerator() => _components.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _components.GetEnumerator();
        }

        [Injector]
        static Injector<AllComponents> Injector => Injectors.Injector.From(world => new AllComponents(world.Components()));
        [Depender]
        static IDepender Depender => Dependers.Depender.From(
            new Dependencies.Read(typeof(Entity)),
            new Dependencies.Write(typeof(IComponent)),
            new Dependencies.Emit(typeof(Messages.OnAdd)),
            new Dependencies.Emit(typeof(Messages.OnAdd<>)),
            new Dependencies.Emit(typeof(Messages.OnRemove)),
            new Dependencies.Emit(typeof(Messages.OnRemove<>)),
            new Dependencies.Emit(typeof(Messages.OnEnable)),
            new Dependencies.Emit(typeof(Messages.OnEnable<>)),
            new Dependencies.Emit(typeof(Messages.OnDisable)),
            new Dependencies.Emit(typeof(Messages.OnDisable<>)));

        readonly Modules.Components _components;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllComponents"/> struct.
        /// </summary>
        /// <param name="components"></param>
        public AllComponents(Modules.Components components) { _components = components; }

        /// <inheritdoc cref="Modules.Components.Default{T}()"/>
        [ThreadSafe]
        public T Default<T>() where T : struct, IComponent => _components.Default<T>();
        /// <inheritdoc cref="Modules.Components.TryDefault(Type, out IComponent)"/>
        [ThreadSafe]
        public bool TryDefault(Type type, out IComponent component) => _components.TryDefault(type, out component);
        /// <inheritdoc cref="Modules.Components.Get{T}(Entity, States)"/>
        [ThreadSafe]
        public ref T Get<T>(Entity entity, States include = States.All) where T : struct, IComponent => ref _components.Get<T>(entity, include);
        /// <inheritdoc cref="Modules.Components.GetOrAdd{T}(Entity, Func{T})"/>
        public ref T GetOrAdd<T>(Entity entity, Func<T> create = null) where T : struct, IComponent => ref _components.GetOrAdd(entity, create);
        /// <inheritdoc cref="Modules.Components.GetOrDummy{T}(Entity, out bool, States)"/>
        [ThreadSafe]
        public ref T GetOrDummy<T>(Entity entity, out bool success, States include = States.All) where T : struct, IComponent => ref _components.GetOrDummy<T>(entity, out success, include);
        /// <inheritdoc cref="Modules.Components.TryGet{T}(Entity, out T, States)"/>
        [ThreadSafe]
        public bool TryGet<T>(Entity entity, out T component, States include = States.All) where T : struct, IComponent => _components.TryGet(entity, out component, include);
        /// <inheritdoc cref="Modules.Components.TryGet(Entity, Type, out IComponent, States)"/>
        [ThreadSafe]
        public bool TryGet(Entity entity, Type type, out IComponent component, States include = States.All) => _components.TryGet(entity, type, out component, include);
        /// <inheritdoc cref="Modules.Components.Get(Entity, States)"/>
        [ThreadSafe]
        public IEnumerable<IComponent> Get(Entity entity, States include = States.All) => _components.Get(entity, include);
        /// <inheritdoc cref="Modules.Components.Get{T}(States)"/>
        [ThreadSafe]
        public IEnumerable<(Entity entity, T component)> Get<T>(States include = States.All) where T : struct, IComponent => _components.Get<T>(include);
        /// <inheritdoc cref="Modules.Components.Get(Type, States)"/>
        [ThreadSafe]
        public IEnumerable<(Entity entity, IComponent component)> Get(Type type, States include = States.All) => _components.Get(type, include);
        /// <inheritdoc cref="Modules.Components.Has{T}(Entity, States)"/>
        [ThreadSafe]
        public bool Has<T>(Entity entity, States include = States.All) where T : IComponent => _components.Has<T>(entity, include);
        /// <inheritdoc cref="Modules.Components.Has(Entity, Type, States)"/>
        [ThreadSafe]
        public bool Has(Entity entity, Type type, States include = States.All) => _components.Has(entity, type, include);
        /// <inheritdoc cref="Modules.Components.Count(Entity, States)"/>
        [ThreadSafe]
        public int Count(Entity entity, States include = States.All) => _components.Count(entity, include);
        /// <inheritdoc cref="Modules.Components.Count{T}(States)"/>
        [ThreadSafe]
        public int Count<T>(States include = States.All) where T : IComponent => _components.Count<T>(include);
        /// <inheritdoc cref="Modules.Components.Count(Type, States)"/>
        [ThreadSafe]
        public int Count(Type type, States include = States.All) => _components.Count(type, include);
        /// <inheritdoc cref="Modules.Components.Count(States)"/>
        [ThreadSafe]
        public int Count(States include = States.All) => _components.Count(include);
        /// <inheritdoc cref="Modules.Components.State{T}(Entity)"/>
        [ThreadSafe]
        public States State<T>(Entity entity) where T : struct, IComponent => _components.State<T>(entity);
        /// <inheritdoc cref="Modules.Components.State(Entity, Type)"/>
        [ThreadSafe]
        public States State(Entity entity, Type type) => _components.State(entity, type);
        /// <inheritdoc cref="Modules.Components.Set{T}(Entity, in T)"/>
        public bool Set<T>(Entity entity, in T component) where T : struct, IComponent => _components.Set(entity, component);
        /// <inheritdoc cref="Modules.Components.Set{T}(Entity)"/>
        public bool Set<T>(Entity entity) where T : struct, IComponent => _components.Set<T>(entity);
        /// <inheritdoc cref="Modules.Components.Set(Entity, IComponent)"/>
        public bool Set(Entity entity, IComponent component) => _components.Set(entity, component);
        /// <inheritdoc cref="Modules.Components.Set(Entity, Type)"/>
        public bool Set(Entity entity, Type type) => _components.Set(entity, type);
        /// <inheritdoc cref="Modules.Components.Remove{T}(Entity, States)"/>
        public bool Remove<T>(Entity entity, States include = States.All) where T : IComponent => _components.Remove<T>(entity, include);
        /// <inheritdoc cref="Modules.Components.Remove(Entity, Type, States)"/>
        public bool Remove(Entity entity, Type type, States include = States.All) => _components.Remove(entity, type, include);
        /// <inheritdoc cref="Modules.Components.Clear{T}(States)"/>
        public bool Clear<T>(States include = States.All) where T : IComponent => _components.Clear<T>(include);
        /// <inheritdoc cref="Modules.Components.Clear(Type, States)"/>
        public bool Clear(Type type, States include = States.All) => _components.Clear(type, include);
        /// <inheritdoc cref="Modules.Components.Clear(Entity, States)"/>
        public bool Clear(Entity entity, States include = States.All) => _components.Clear(entity, include);
        /// <inheritdoc cref="Modules.Components.Clear(States)"/>
        public bool Clear(States include = States.All) => _components.Clear(include);
        /// <inheritdoc cref="Modules.Components.Copy{T}(Entity, Entity, States)"/>
        public bool Copy<T>(Entity source, Entity target, States include = States.All) where T : IComponent => _components.Copy<T>(source, target, include);
        /// <inheritdoc cref="Modules.Components.Copy(Entity, Entity, Type, States)"/>
        public bool Copy(Entity source, Entity target, Type type, States include = States.All) => _components.Copy(source, target, type, include);
        /// <inheritdoc cref="Modules.Components.Copy(Entity, Entity, States)"/>
        public bool Copy(Entity source, Entity target, States include = States.All) => _components.Copy(source, target, include);
        /// <inheritdoc cref="Modules.Components.Trim(Entity, Entity, States)"/>
        public bool Trim(Entity source, Entity target, States include = States.All) => _components.Trim(source, target, include);
        /// <inheritdoc cref="Modules.Components.Enable{T}(Entity)"/>
        public bool Enable<T>(Entity entity) where T : struct, IComponent => _components.Enable<T>(entity);
        /// <inheritdoc cref="Modules.Components.Enable(Entity, Type)"/>
        public bool Enable(Entity entity, Type type) => _components.Enable(entity, type);
        /// <inheritdoc cref="Modules.Components.Enable(Entity)"/>
        public bool Enable(Entity entity) => _components.Enable(entity);
        /// <inheritdoc cref="Modules.Components.Disable{T}(Entity)"/>
        public bool Disable<T>(Entity entity) where T : struct, IComponent => _components.Disable<T>(entity);
        /// <inheritdoc cref="Modules.Components.Disable(Entity, Type)"/>
        public bool Disable(Entity entity, Type type) => _components.Disable(entity, type);
        /// <inheritdoc cref="Modules.Components.Disable(Entity)"/>
        public bool Disable(Entity entity) => _components.Disable(entity);
        /// <inheritdoc cref="Modules.Components.GetEnumerator()"/>
        [ThreadSafe]
        public IEnumerator<IComponent> GetEnumerator() => _components.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _components.GetEnumerator();
    }

    /// <summary>
    /// Gives access to component operations for type <typeparamref name="T"/>.
    /// </summary>
    public readonly struct Components<T> : IInjectable, IEnumerable<(Entity entity, T component)> where T : struct, IComponent
    {
        /// <inheritdoc cref="Components{T}"/>
        [ThreadSafe]
        public readonly struct Write : IInjectable, IEnumerable<(Entity entity, T component)>
        {
            [Injector]
            static Injector<Write> Injector => Injectors.Injector.From(world => new Write(world.Components()));
            [Depender]
            static IDepender Depender => Dependers.Depender.From<T>(
                new Dependencies.Read(typeof(Entity)),
                new Dependencies.Write(typeof(T)));

            readonly Modules.Components _components;

            /// <summary>
            /// Initializes a new instance of the <see cref="Components{T}.Write" /> struct.
            /// </summary>
            /// <param name="components"></param>
            public Write(Modules.Components components) { _components = components; }

            /// <inheritdoc cref="Modules.Components.Default{T}()"/>
            public T Default() => _components.Default<T>();
            /// <inheritdoc cref="Modules.Components.GetOrDummy{T}(Entity, out bool, States)"/>
            public ref T GetOrDummy(Entity entity, out bool success, States include = States.All) => ref _components.GetOrDummy<T>(entity, out success, include);
            /// <inheritdoc cref="Modules.Components.TryGet{T}(Entity, out T, States)"/>
            public bool TryGet(Entity entity, out T component, States include = States.All) => _components.TryGet(entity, out component, include);
            /// <inheritdoc cref="Modules.Components.Get{T}(Entity, States)"/>
            public ref T Get(Entity entity, States include = States.All) => ref _components.Get<T>(entity, include);
            /// <inheritdoc cref="Modules.Components.Has{T}(Entity, States)"/>
            public bool Has(Entity entity, States include = States.All) => _components.Has<T>(entity, include);
            /// <inheritdoc cref="Modules.Components.Count{T}(States)"/>
            public int Count(States include = States.All) => _components.Count<T>(include);
            /// <inheritdoc cref="Modules.Components.State{T}(Entity)"/>
            public States State(Entity entity) => _components.State<T>(entity);
            /// <inheritdoc cref="Modules.Components.GetEnumerator()"/>
            public IEnumerator<(Entity entity, T component)> GetEnumerator() => _components.Get<T>(States.All).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <inheritdoc cref="Components{T}"/>
        [ThreadSafe]
        public readonly struct Read : IInjectable, IEnumerable<(Entity entity, T component)>
        {
            [Injector]
            static Injector<Read> Injector => Injectors.Injector.From(world => new Read(world.Components()));
            [Depender]
            static IDepender Depender => Dependers.Depender.From<T>(
                new Dependencies.Read(typeof(Entity)),
                new Dependencies.Read(typeof(T)));

            readonly Modules.Components _components;

            /// <summary>
            /// Initializes a new instance of the <see cref="Components{T}.Read" /> struct.
            /// </summary>
            /// <param name="components"></param>
            public Read(Modules.Components components) { _components = components; }

            /// <inheritdoc cref="Modules.Components.Default{T}()"/>
            public T Default() => _components.Default<T>();
            /// <inheritdoc cref="Modules.Components.GetOrDummy{T}(Entity, out bool, States)"/>
            public ref readonly T GetOrDummy(Entity entity, out bool success, States include = States.All) => ref _components.GetOrDummy<T>(entity, out success, include);
            /// <inheritdoc cref="Modules.Components.TryGet{T}(Entity, out T, States)"/>
            public bool TryGet(Entity entity, out T component, States include = States.All) => _components.TryGet(entity, out component, include);
            /// <inheritdoc cref="Modules.Components.Get{T}(Entity, States)"/>
            public ref readonly T Get(Entity entity, States include = States.All) => ref _components.Get<T>(entity, include);
            /// <inheritdoc cref="Modules.Components.Has{T}(Entity, States)"/>
            public bool Has(Entity entity, States include = States.All) => _components.Has<T>(entity, include);
            /// <inheritdoc cref="Modules.Components.Count{T}(States)"/>
            public int Count(States include = States.All) => _components.Count<T>(include);
            /// <inheritdoc cref="Modules.Components.State{T}(Entity)"/>
            public States State(Entity entity) => _components.State<T>(entity);
            /// <inheritdoc cref="Modules.Components.GetEnumerator()"/>
            public IEnumerator<(Entity entity, T component)> GetEnumerator() => _components.Get<T>(States.All).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Injector]
        static Injector<Components<T>> Injector => Injectors.Injector.From(world => new Components<T>(world.Components()));
        [Depender]
        static IDepender Depender => Dependers.Depender.From<T>(
            new Dependencies.Read(typeof(Entity)),
            new Dependencies.Write(typeof(T)),
            new Dependencies.Emit(typeof(Messages.OnAdd)),
            new Dependencies.Emit(typeof(Messages.OnAdd<T>)),
            new Dependencies.Emit(typeof(Messages.OnRemove)),
            new Dependencies.Emit(typeof(Messages.OnRemove<T>)),
            new Dependencies.Emit(typeof(Messages.OnEnable)),
            new Dependencies.Emit(typeof(Messages.OnEnable<T>)),
            new Dependencies.Emit(typeof(Messages.OnDisable)),
            new Dependencies.Emit(typeof(Messages.OnDisable<T>)));

        readonly Modules.Components _components;

        /// <summary>
        /// Initializes a new instance of the <see cref="Components{T}" /> struct.
        /// </summary>
        /// <param name="components"></param>
        public Components(Modules.Components components) { _components = components; }

        /// <inheritdoc cref="Modules.Components.Default{T}()"/>
        [ThreadSafe]
        public T Default() => _components.Default<T>();
        /// <inheritdoc cref="Modules.Components.GetOrAdd{T}(Entity, Func{T})"/>
        public ref T GetOrAdd(Entity entity, Func<T> create = null) => ref _components.GetOrAdd(entity, create);
        /// <inheritdoc cref="Modules.Components.GetOrDummy{T}(Entity, out bool, States)"/>
        [ThreadSafe]
        public ref T GetOrDummy(Entity entity, out bool success, States include = States.All) => ref _components.GetOrDummy<T>(entity, out success, include);
        /// <inheritdoc cref="Modules.Components.TryGet{T}(Entity, out T, States)"/>
        [ThreadSafe]
        public bool TryGet(Entity entity, out T component, States include = States.All) => _components.TryGet(entity, out component, include);
        /// <inheritdoc cref="Modules.Components.Get{T}(Entity, States)"/>
        [ThreadSafe]
        public ref T Get(Entity entity, States include = States.All) => ref _components.Get<T>(entity, include);
        /// <inheritdoc cref="Modules.Components.Has{T}(Entity, States)"/>
        [ThreadSafe]
        public bool Has(Entity entity, States include = States.All) => _components.Has<T>(entity, include);
        /// <inheritdoc cref="Modules.Components.Count{T}(States)"/>
        [ThreadSafe]
        public int Count(States include = States.All) => _components.Count<T>(include);
        /// <inheritdoc cref="Modules.Components.State{T}(Entity)"/>
        [ThreadSafe]
        public States State(Entity entity) => _components.State<T>(entity);
        /// <inheritdoc cref="Modules.Components.Set{T}(Entity)"/>
        public bool Set(Entity entity) => _components.Set<T>(entity);
        /// <inheritdoc cref="Modules.Components.Set{T}(Entity, in T)"/>
        public bool Set(Entity entity, in T component) => _components.Set(entity, component);
        /// <inheritdoc cref="Modules.Components.Remove{T}(Entity, States)"/>
        public bool Remove(Entity entity, States include = States.All) => _components.Remove<T>(entity, include);
        /// <inheritdoc cref="Modules.Components.Clear{T}(States)"/>
        public bool Clear(States include = States.All) => _components.Clear<T>(include);
        /// <inheritdoc cref="Modules.Components.Copy{T}(Entity, Entity, States)"/>
        public bool Copy(Entity source, Entity target, States include = States.All) => _components.Copy<T>(source, target, include);
        /// <inheritdoc cref="Modules.Components.Enable{T}(Entity)"/>
        public bool Enable(Entity entity) => _components.Enable<T>(entity);
        /// <inheritdoc cref="Modules.Components.Disable{T}(Entity)"/>
        public bool Disable(Entity entity) => _components.Disable<T>(entity);
        /// <inheritdoc cref="Modules.Components.GetEnumerator()"/>
        [ThreadSafe]
        public IEnumerator<(Entity entity, T component)> GetEnumerator() => _components.Get<T>(States.All).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
