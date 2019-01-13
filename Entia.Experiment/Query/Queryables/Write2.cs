using System;
using Entia.Core;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers2;
using Entia.Queryables;

namespace Entia.Queryables2
{
    public readonly unsafe struct Write2<T> : IQueryable where T : struct, IComponent
    {
        sealed class Querier : IQuerier<Write2<T>>
        {
            public bool TryQuery(Segment segment, World world, out Query2<Write2<T>> query)
            {
                if (segment.Has<T>())
                {
                    var metadata = ComponentUtility.Cache<T>.Data;
                    query = new Query2<Write2<T>>(index => new Write2<T>(segment.Store<T>(), index), new[] { metadata });
                    return true;
                }

                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        public ref T Value => ref _array[_index.Value];

        readonly T[] _array;
        readonly Box<int> _index;

        public Write2(T[] array, Box<int> index)
        {
            _array = array;
            _index = index;
        }
    }
}
