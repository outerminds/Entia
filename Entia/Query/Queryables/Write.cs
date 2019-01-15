using System;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Dependables;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia.Queryables
{
    public readonly struct Write<T> : IQueryable, IDepend<Dependables.Write<T>> where T : struct, IComponent
    {
        sealed class Querier : Querier<Write<T>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<Write<T>> query)
            {
                if (segment.Has<T>())
                {
                    var metadata = ComponentUtility.Cache<T>.Data;
                    query = new Query<Write<T>>(index => new Write<T>(segment.GetStore<T>(), index), metadata);
                    return true;
                }

                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

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
