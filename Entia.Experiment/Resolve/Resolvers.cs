using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Experiment.Resolvables;
using Entia.Experiment.Resolvers;
using Entia.Modules;

namespace Entia.Experiment.Modules
{
    public sealed class Resolvers : IModule, IResolvable
    {
        struct Data
        {
            public Action<Array, int> Resolve;
            public Array Resolvables;
            public int Count;
        }

        public IEnumerable<IResolvablez> Resolvables
        {
            get
            {
                for (int i = 0; i < _queue.count; i++)
                {
                    var pair = _queue.items[i];
                    var data = _data[pair.data];
                    yield return data.Resolvables.GetValue(pair.resolvable) as IResolvablez;
                }
            }
        }

        readonly TypeMap<IResolvablez, IResolver> _defaults = new TypeMap<IResolvablez, IResolver>();
        readonly TypeMap<IResolvablez, Data> _data = new TypeMap<IResolvablez, Data>();
        ((int data, int resolvable)[] items, int count) _queue = (new (int, int)[32], 0);

        public IResolver<T> Default<T>() where T : struct, IResolvablez =>
            _defaults.Default(typeof(T), typeof(IResolvablez<>), typeof(ResolverAttribute), () => new Default<T>()) as IResolver<T>;

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

        public void Defer<T>(in T resolvable) where T : struct, IResolvablez
        {
            var dataIndex = TypeMap<IResolvablez, Data>.Cache<T>.Index;
            ref var data = ref _data.Get(dataIndex, out var success);
            if (success && data.Resolvables is T[] resolvables)
            {
                var index = data.Count++;
                ArrayUtility.TryAdd(ref data.Resolvables, resolvable, index);
                _queue.Push((dataIndex, index));
            }
            else
            {
                _data.Set(dataIndex, CreateData(resolvable));
                _queue.Push((dataIndex, 0));
            }
        }

        public void Defer<TState>(in TState state, Action<TState> @do) => Defer(new Do<TState>(state, @do));

        Data CreateData<T>(in T resolvable) where T : struct, IResolvablez
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