using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectables;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia
{
    public sealed class World : IInjectable, IEnumerable<TypeMap<IModule, IModule>.ValueEnumerator, IModule>
    {
        struct State
        {
            public Dictionary<ulong, WeakReference<World>> Worlds;
            public ulong Count;
        }

        [Injector]
        static Injector<World> Injector => Injectors.Injector.From(world => world);
        [Depender]
        static IDepender Depender => Dependers.Depender.From(new Dependencies.Unknown());

        static readonly Concurrent<State> _state = new State { Worlds = new Dictionary<ulong, WeakReference<World>>() };

        [ThreadSafe]
        public static World[] Instances() => Instances(_ => true);

        [ThreadSafe]
        public static World[] Instances(Func<World, bool> predicate) => Instances(predicate, (world, state) => state(world));

        [ThreadSafe]
        public static World[] Instances<TState>(in TState state, Func<World, TState, bool> predicate)
        {
            using (var write = _state.Write())
            {
                var worlds = new List<World>(write.Value.Worlds.Count);
                foreach (var reference in write.Value.Worlds.Values)
                    if (reference.TryGetTarget(out var world) && predicate(world, state)) worlds.Add(world);
                return worlds.ToArray();
            }
        }

        [ThreadSafe]
        public static bool TryInstance(Func<World, bool> predicate, out World world) =>
            TryInstance(predicate, (instance, state) => state(instance), out world);

        [ThreadSafe]
        public static bool TryInstance<TState>(in TState state, Func<World, TState, bool> predicate, out World world)
        {
            using (var write = _state.Write())
            {
                foreach (var reference in write.Value.Worlds.Values)
                    if (reference.TryGetTarget(out world) && predicate(world, state)) return true;

                world = default;
                return false;
            }
        }

        readonly ulong _identifier;
        readonly TypeMap<IModule, IModule> _modules = new TypeMap<IModule, IModule>();
        IResolvable[] _resolvables = Array.Empty<IResolvable>();

        public World()
        {
            using (var write = _state.Write())
            {
                _identifier = ++write.Value.Count;
                write.Value.Worlds[_identifier] = new WeakReference<World>(this);
            }
        }

        ~World()
        {
            using (var write = _state.Write()) write.Value.Worlds.Remove(_identifier);
        }

        public bool TryGet<T>(out T module) where T : IModule
        {
            if (_modules.TryGet<T>(out var value, false, false) && value is T casted)
            {
                module = casted;
                return true;
            }

            module = default;
            return false;
        }
        public bool Set<T>(T module) where T : IModule
        {
            Remove<T>();
            if (module is IResolvable resolvable) ArrayUtility.Add(ref _resolvables, resolvable);
            return _modules.Set<T>(module);
        }
        public bool Has<T>() where T : IModule => _modules.Has<T>(false, false);
        public bool Remove<T>() where T : IModule
        {
            if (_modules.TryGet<T>(out var module, false, false) && module is IResolvable resolvable) ArrayUtility.Remove(ref _resolvables, resolvable);
            return _modules.Remove<T>(false, false);
        }
        public bool Clear()
        {
            _resolvables = Array.Empty<IResolvable>();
            return _modules.Clear();
        }
        public bool Resolve()
        {
            var resolved = false;
            for (int i = 0; i < _resolvables.Length; i++) resolved |= _resolvables[i].Resolve();
            return resolved;
        }

        public override string ToString() =>
            TryGet<Modules.Resources>(out var resources) && resources.TryGet<Resources.Debug>(out var debug) ?
            debug.Name : base.ToString();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public TypeMap<IModule, IModule>.ValueEnumerator GetEnumerator() => _modules.Values.GetEnumerator();
        IEnumerator<IModule> IEnumerable<IModule>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
