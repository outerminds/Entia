using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Resolvables;
using Entia.Resolvers;

namespace Entia.Modules
{
    public delegate bool Resolve<T>(in T resolvable);

    public sealed class Resolvers : IModule, IResolvable
    {
        struct Data
        {
            public Array Resolvables;
            public int Count;
            public readonly Func<Array, int, bool> Resolve;
            public readonly object Lock;

            public Data(Array resolvables, int count, Func<Array, int, bool> resolve)
            {
                Resolvables = resolvables;
                Count = count;
                Resolve = resolve;
                Lock = new object();
            }
        }

        [ThreadSafe]
        public int Count => _queue.Count;
        [ThreadSafe]
        public IEnumerable<Resolvables.IResolvable> Resolvables
        {
            get
            {
                foreach (var pair in _queue)
                {
                    var data = _data[pair.data];
                    yield return data.Resolvables.GetValue(pair.resolvable) as Resolvables.IResolvable;
                }
            }
        }

        readonly World _world;
        readonly TypeMap<Resolvables.IResolvable, Data> _data = new TypeMap<Resolvables.IResolvable, Data>();
        readonly ConcurrentQueue<(int data, int resolvable)> _queue = new ConcurrentQueue<(int data, int resolvable)>();

        public Resolvers(World world) { _world = world; }

        public bool Resolve()
        {
            // NOTE: with this implementation, if a resolution causes to enqueue a new resolvable, it is going to be resolved within this call making it vulnerable to infinite loops;
            // this may be the desired behaviour but an alternative would be to have an 'active' queue and a 'pending' queue and switch the two when resolving
            var resolved = false;
            while (_queue.TryDequeue(out var pair))
            {
                ref var data = ref _data[pair.data];
                data.Count--;
                resolved |= data.Resolve(data.Resolvables, pair.resolvable);
            }
            return resolved;
        }

        [ThreadSafe]
        public bool Defer<T>(in T resolvable) where T : struct, Resolvables.IResolvable
        {
            var dataIndex = _data.Index<T>();
            ref var data = ref _data.Get(dataIndex, out var success);
            if (success && Add(resolvable, ref data, out var index))
            {
                _queue.Enqueue((dataIndex, index));
                return true;
            }

            lock (_data)
            {
                // NOTE: if this is true, it means that multiple threads were waiting at the lock
                if (_data.Has(dataIndex)) return Defer(resolvable);
                else if (TryCreateData(resolvable, out var created)) _data.Set(dataIndex, created);
                else return false;
            }
            _queue.Enqueue((dataIndex, 0));
            return true;
        }

        [ThreadSafe]
        public bool Defer(Action @do) => Defer(@do, action => action());
        [ThreadSafe]
        public bool Defer(Action<World> @do) => Defer(_world, @do);
        [ThreadSafe]
        public bool Defer<T>(in T state, Action<T> @do) => Defer(new Do<T>(state, @do));
        [ThreadSafe]
        public bool Defer<T>(in T state, Action<T, World> @do) => Defer((state, world: _world, @do), input => input.@do(input.state, input.world));

        [ThreadSafe]
        bool Add<T>(in T resolvable, ref Data data, out int index) where T : struct, Resolvables.IResolvable
        {
            lock (data.Lock)
            {
                if (data.Resolvables is T[] resolvables)
                {
                    index = data.Count++;
                    if (ArrayUtility.Ensure(ref resolvables, data.Count)) data.Resolvables = resolvables;
                    resolvables[index] = resolvable;
                    return true;
                }
            }
            index = default;
            return false;
        }

        [ThreadSafe]
        bool TryCreateData<T>(in T resolvable, out Data data) where T : struct, Resolvables.IResolvable
        {
            if (_world.Container.TryGet<T, Resolver<T>>(out var resolver))
            {
                var resolve = new Resolve<T>(resolver.Resolve);
                var resolvables = new T[8];
                resolvables[0] = resolvable;
                data = new Data(resolvables, 1, (array, index) => resolve(((T[])array)[index]));
                return true;
            }
            data = default;
            return false;
        }
    }
}