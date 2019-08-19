using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Resolvables;
using Entia.Resolvers;

namespace Entia.Modules
{
    public delegate bool Resolve<T>(in T resolvable);

    public sealed class Resolvers : IModule, Modules.IResolvable
    {
        struct Data
        {
            public Func<Array, int, bool> Resolve;
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

        readonly World _world;
        readonly TypeMap<Resolvables.IResolvable, Data> _data = new TypeMap<Resolvables.IResolvable, Data>();
        ((int data, int resolvable)[] items, int count) _queue = (new (int, int)[32], 0);

        public Resolvers(World world) { _world = world; }

        public bool Resolve()
        {
            var resolved = false;
            // NOTE: with this implementation, if a resolution causes to enqueue a new resolvable, it is going to be resolved within this call making it vulnerable to infinite loops;
            // this may be the desired behaviour but an alternative would be to have an 'active' queue and a 'pending' queue and switch the two when resolving
            for (int i = 0; i < _queue.count; i++)
            {
                var pair = _queue.items[i];
                ref var data = ref _data[pair.data];
                data.Count--;
                resolved |= data.Resolve(data.Resolvables, pair.resolvable);
            }
            return _queue.count.Change(0) || resolved;
        }

        public bool Defer<T>(in T resolvable) where T : struct, Resolvables.IResolvable
        {
            var dataIndex = _data.Index<T>();
            ref var data = ref _data.Get(dataIndex, out var success);
            if (success && data.Resolvables is T[] resolvables)
            {
                var index = data.Count++;
                ArrayUtility.EnsureSet(ref data.Resolvables, resolvable, index);
                _queue.Push((dataIndex, index));
                return true;
            }
            else if (TryCreateData(resolvable, out var created))
            {
                _data.Set(dataIndex, created);
                _queue.Push((dataIndex, 0));
                return true;
            }
            return false;
        }

        public bool Defer(Action @do) => Defer(@do, action => action());
        public bool Defer(Action<World> @do) => Defer(_world, @do);
        public bool Defer<T>(in T state, Action<T> @do) => Defer(new Do<T>(state, @do));
        public bool Defer<T>(in T state, Action<T, World> @do) => Defer((state, world: _world, @do), input => input.@do(input.state, input.world));

        bool TryCreateData<T>(in T resolvable, out Data data) where T : struct, Resolvables.IResolvable
        {
            if (_world.Container.TryGet<T, Resolver<T>>(out var resolver))
            {
                var resolve = new Resolve<T>(resolver.Resolve);
                var resolvables = new T[8];
                resolvables[0] = resolvable;
                data = new Data
                {
                    Resolve = (array, index) => resolve(((T[])array)[index]),
                    Resolvables = resolvables,
                    Count = 1
                };
                return true;
            }
            data = default;
            return false;
        }
    }
}