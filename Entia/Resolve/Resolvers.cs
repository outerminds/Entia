using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Resolvables;
using Entia.Resolvers;
using Entia.Modules;
using System.Linq;
using System.Collections;

namespace Entia.Modules
{
    public delegate void Resolve<T>(in T resolvable);

    public sealed class Resolvers : IModule, Modules.IResolvable, IEnumerable<IResolver>
    {
        struct Data
        {
            public Action<Array, int> Resolve;
            public Array Resolvables;
            public int Count;
        }

        public int Count => _queue.count;
        public IEnumerable<Resolvables.IResolvable> Resolvables
        {
            get
            {
                for (int i = 0; i < _queue.count; i++)
                {
                    var pair = _queue.items[i];
                    var data = _data[pair.data];
                    yield return data.Resolvables.GetValue(pair.resolvable) as Resolvables.IResolvable;
                }
            }
        }

        readonly TypeMap<Resolvables.IResolvable, IResolver> _defaults = new TypeMap<Resolvables.IResolvable, IResolver>();
        readonly TypeMap<Resolvables.IResolvable, IResolver> _resolvers = new TypeMap<Resolvables.IResolvable, IResolver>();
        readonly TypeMap<Resolvables.IResolvable, Data> _data = new TypeMap<Resolvables.IResolvable, Data>();
        readonly World _world;
        ((int data, int resolvable)[] items, int count) _queue = (new (int, int)[32], 0);

        public Resolvers(World world) { _world = world; }

        public void Resolve()
        {
            // NOTE: with this implementation, if a resolution causes to enqueue a new resolvable, it is going to be resolved within this call making it vulnerable to infinite loops;
            // this may be the desired behaviour but an alternative would be to have an 'active' queue and a 'pending' queue and switch the two when resolving
            for (int i = 0; i < _queue.count; i++)
            {
                var pair = _queue.items[i];
                ref var data = ref _data[pair.data];
                data.Count--;
                data.Resolve(data.Resolvables, pair.resolvable);
            }
            _queue.count = 0;
        }

        public void Defer<T>(in T resolvable) where T : struct, Resolvables.IResolvable
        {
            var dataIndex = TypeMap<Resolvables.IResolvable, Data>.Cache<T>.Index;
            ref var data = ref _data.Get(dataIndex, out var success);
            if (success && data.Resolvables is T[] resolvables)
            {
                var index = data.Count++;
                ArrayUtility.EnsureSet(ref data.Resolvables, resolvable, index);
                _queue.Push((dataIndex, index));
            }
            else
            {
                _data.Set(dataIndex, CreateData(resolvable));
                _queue.Push((dataIndex, 0));
            }
        }

        public void Defer<T>(in T state, Action<T> @do) => Defer(new Do<T>(state, @do));
        public void Defer<T>(in T state, Action<T, World> @do) => Defer((state, world: _world, @do), input => input.@do(input.state, input.world));
        public void Defer<T>(Action<World> @do) => Defer(_world, @do);

        public Resolver<T> Default<T>() where T : struct, Resolvables.IResolvable =>
            _defaults.Default(typeof(T), typeof(Resolvables.IResolvable<>), typeof(ResolverAttribute), () => new Default<T>()) as Resolver<T>;
        public IResolver Default(Type resolvable) =>
            _defaults.Default(resolvable, typeof(Resolvables.IResolvable<>), typeof(ResolverAttribute), typeof(Default<>));

        public bool Has<T>() where T : struct, Resolvables.IResolvable => _resolvers.Has<T>();
        public bool Has(Type resolvable) => _resolvers.Has(resolvable);
        public Resolver<T> Get<T>() where T : struct, Resolvables.IResolvable => _resolvers.TryGet<T>(out var resolver) && resolver is Resolver<T> casted ? casted : Default<T>();
        public IResolver Get(Type resolvable) => _resolvers.TryGet(resolvable, out var resolver) ? resolver : Default(resolvable);
        public bool Set<T>(Resolver<T> resolver) where T : struct, Resolvables.IResolvable => _resolvers.Set<T>(resolver);
        public bool Set(Type resolvable, IResolver resolver) => _resolvers.Set(resolvable, resolver);
        public bool Remove<T>() where T : struct, Resolvables.IResolvable => _resolvers.Remove<T>();
        public bool Remove(Type resolvable) => _resolvers.Remove(resolvable);
        public bool Clear() => _defaults.Clear() | _resolvers.Clear();
        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IResolver> GetEnumerator() => _resolvers.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        Data CreateData<T>(in T resolvable) where T : struct, Resolvables.IResolvable
        {
            var resolver = Default<T>();
            Resolve<T> resolve = resolver.Resolve;
            var resolvables = new T[8];
            resolvables[0] = resolvable;
            return new Data
            {
                Resolve = (array, index) => resolve(((T[])array)[index]),
                Resolvables = resolvables,
                Count = 1
            };
        }
    }
}