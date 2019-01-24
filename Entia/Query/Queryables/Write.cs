using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia.Queryables
{
    [ThreadSafe]
    public readonly struct Write<T> : IQueryable where T : struct, IComponent
    {
        sealed class Querier : Querier<Write<T>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<Write<T>> query)
            {
                ref readonly var metadata = ref ComponentUtility.Concrete<T>.Data;
                if (segment.Mask.Has(metadata.Index))
                {
                    query = new Query<Write<T>>(index => new Write<T>(segment.Store<T>(), index), metadata);
                    return true;
                }

                query = default;
                return false;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                yield return new Write(typeof(T));
                foreach (var dependency in world.Dependers().Dependencies<T>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[_index];
        }

        readonly T[] _array;
        readonly int _index;

        public Write(T[] array, int index)
        {
            _array = array;
            _index = index;
        }
    }
}
