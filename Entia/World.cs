using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependers;
using Entia.Injectables;
using Entia.Injectors;
using Entia.Modules;
using Entia.Experimental.Serializers;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;

namespace Entia
{
    public sealed class World : IInjectable, IEnumerable<TypeMap<IModule, IModule>.ValueEnumerator, IModule>
    {
        [Implementation]
        static Injector<World> _injector => Injector.From(context => context.World);
        [Implementation]
        static IDepender _depender => Depender.From(new Dependencies.Unknown());
        [Implementation]
        static Serializer<World> _serializer => Serializer.Object(
            () => new World(),
            Serializer.Member.Property(
                (in World world) => world._modules.Values.ToArray(),
                (ref World world, in IModule[] modules) => { for (int i = 0; i < modules.Length; i++) world.Set(modules[i]); })
        );

        static readonly Func<bool> _empty = () => true;
        static readonly ConcurrentDictionary<long, WeakReference<World>> _worlds = new ConcurrentDictionary<long, WeakReference<World>>();
        static long _count;

        [ThreadSafe]
        public static IEnumerable<World> Instances()
        {
            foreach (var pair in _worlds)
                if (pair.Value.TryGetTarget(out var world))
                    yield return world;
        }

        [ThreadSafe]
        public static bool TryInstance(Func<World, bool> predicate, out World world)
        {
            foreach (var pair in _worlds)
                if (pair.Value.TryGetTarget(out world) && predicate(world))
                    return true;

            world = default;
            return false;
        }

        public readonly Container Container = new Container();

        readonly long _identifier;
        readonly TypeMap<IModule, IModule> _modules = new TypeMap<IModule, IModule>();
        Func<bool> _resolve = _empty;

        public World(params IModule[] modules)
        {
            _identifier = Interlocked.Increment(ref _count);
            _worlds.TryAdd(_identifier, new WeakReference<World>(this));
            for (int i = 0; i < modules.Length; i++) Set(modules[i]);
        }

        ~World() { _worlds.TryRemove(_identifier, out _); }

        public bool TryGet<T>(out T module) where T : IModule
        {
            if (_modules.TryGet<T>(out var value) && value is T casted)
            {
                module = casted;
                return true;
            }

            module = default;
            return false;
        }

        public bool TryGet(Type type, out IModule module) => _modules.TryGet(type, out module);

        public bool Set<T>(T module) where T : IModule
        {
            Remove<T>();
            if (module is IResolvable resolvable) _resolve += resolvable.Resolve;
            return _modules.Set<T>(module);
        }

        public bool Set(IModule module)
        {
            var type = module.GetType();
            Remove(type);
            if (module is IResolvable resolvable) _resolve += resolvable.Resolve;
            return _modules.Set(type, module);
        }

        public bool Has<T>() where T : IModule => _modules.Has<T>();
        public bool Has(Type type) => _modules.Has(type);

        public bool Remove<T>() where T : IModule
        {
            if (_modules.TryGet<T>(out var module) && module is IResolvable resolvable) _resolve -= resolvable.Resolve;
            return _modules.Remove<T>();
        }

        public bool Remove(Type type)
        {
            if (_modules.TryGet(type, out var module) && module is IResolvable resolvable) _resolve -= resolvable.Resolve;
            return _modules.Remove(type);
        }

        public bool Clear()
        {
            _resolve = _empty;
            return _modules.Clear();
        }

        public bool Resolve() => _resolve();

        public override string ToString() =>
            TryGet<Modules.Resources>(out var resources) && resources.TryGet<Resources.Debug>(out var debug) ?
            debug.Name : base.ToString();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public TypeMap<IModule, IModule>.ValueEnumerator GetEnumerator() => _modules.Values.GetEnumerator();
        IEnumerator<IModule> IEnumerable<IModule>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
