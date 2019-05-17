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
    public readonly struct Read<T> : IQueryable where T : struct, IComponent
    {
        sealed class Querier : Querier<Read<T>>
        {
            public override bool TryQuery(in Context context, out Query<Read<T>> query)
            {
                ref readonly var metadata = ref ComponentUtility.Cache<T>.Data;
                var segment = context.Segment;
                if (context.World.Components().Has(segment.Mask, metadata, context.Include))
                {
                    query = new Query<Read<T>>(index => new Read<T>(segment.Store<T>(), index), metadata);
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
                yield return new Read(typeof(T));
                foreach (var dependency in world.Dependers().Dependencies<T>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public ref readonly T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[_index];
        }

        readonly T[] _array;
        readonly int _index;

        public Read(T[] array, int index)
        {
            _array = array;
            _index = index;
        }
    }
}
